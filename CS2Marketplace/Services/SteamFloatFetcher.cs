using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.Internal;
using SteamKit2.GC;
using SteamKit2.GC.CSGO.Internal;

namespace CS2Marketplace.Services
{
    /// <summary>
    /// A "faster" SteamFloatFetcher that:
    /// 1) Uses a dedicated background thread to continuously pump callbacks.
    /// 2) Waits for the GC to be ready (k_EMsgGCClientWelcome or HAVE_SESSION) before sending float requests.
    /// 3) Supports one float request at a time (GC response does not echo request parameters).
    /// 4) Sets the persona state to Online when account info is received.
    /// </summary>
    public class SteamFloatFetcher : IDisposable
    {
        private readonly SteamClient _client;
        private readonly CallbackManager _callbackManager;
        private SteamUser _user;
        private SteamFriends _friends;
        private SteamGameCoordinator _gc;
        private readonly SemaphoreSlim _floatRequestSemaphore = new SemaphoreSlim(1, 1);


        private bool _isConnected;
        private bool _isLoggedOn;
        private bool _gcReady;

        private readonly string _username;
        private readonly string _password;

        // A single pending float request. Null means no request in flight.
        private TaskCompletionSource<float?> _pendingRequestTcs = null;

        // A background thread that continuously calls _callbackManager.RunWaitCallbacks().
        private Thread _callbackPumpThread;
        private volatile bool _runCallbackPump;

        // 730 is CS:GO.
        private const uint APPID_CSGO = 730;

        public SteamFloatFetcher(string username, string password)
        {
            _username = username;
            _password = password;

            _client = new SteamClient();
            _callbackManager = new CallbackManager(_client);

            // Get handlers
            _user = _client.GetHandler<SteamUser>();
            _friends = _client.GetHandler<SteamFriends>();
            _gc = _client.GetHandler<SteamGameCoordinator>();

            // Subscribe to basic Steam callbacks
            _callbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            _callbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
            _callbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);

            // Subscribe to AccountInfoCallback to set persona online.
            _callbackManager.Subscribe<SteamUser.AccountInfoCallback>(OnAccountInfo);

            // Subscribe to GC messages exactly once.
            _callbackManager.Subscribe<SteamGameCoordinator.MessageCallback>(OnGCMessage);
        }

        private void OnConnected(SteamClient.ConnectedCallback cb)
        {
            Console.WriteLine("Connected to Steam (TCP).");
            _isConnected = true;
        }

        private void OnDisconnected(SteamClient.DisconnectedCallback cb)
        {
            Console.WriteLine("Disconnected from Steam.");
            _isConnected = false;
            _isLoggedOn = false;
            _gcReady = false;
        }

        private void OnLoggedOn(SteamUser.LoggedOnCallback cb)
        {
            if (cb.Result == EResult.OK)
            {
                Console.WriteLine("Logged on to Steam successfully.");
                _isLoggedOn = true;
            }
            else
            {
                Console.WriteLine($"Logon failed: {cb.Result}");
            }
        }

        private void OnAccountInfo(SteamUser.AccountInfoCallback cb)
        {
            // Set persona state to Online.
            Console.WriteLine("Received AccountInfo; setting persona state to Online.");
            _friends.SetPersonaState(EPersonaState.Online);
        }

        /// <summary>
        /// Our single GC callback handler.
        /// It marks GC readiness when it sees welcome/HAVE_SESSION messages and, when a float response arrives,
        /// completes the pending TaskCompletionSource.
        /// </summary>
        private void OnGCMessage(SteamGameCoordinator.MessageCallback callback)
        {
            if (callback.EMsg == (uint)EGCBaseClientMsg.k_EMsgGCClientWelcome)
            {
                Console.WriteLine("GC is ready (k_EMsgGCClientWelcome).");
                _gcReady = true;
                return;
            }
            if (callback.EMsg == (uint)EGCBaseClientMsg.k_EMsgGCClientConnectionStatus)
            {
                using var ms = new MemoryStream(callback.Message.GetData());
                var connStatus = ProtoBuf.Serializer.Deserialize<CMsgConnectionStatus>(ms);
                if ((GCConnectionStatus)connStatus.status == GCConnectionStatus.GCConnectionStatus_HAVE_SESSION)
                {
                    Console.WriteLine("GC is ready (HAVE_SESSION).");
                    _gcReady = true;
                }
                return;
            }
            if (callback.EMsg == (uint)ECsgoGCMsg.k_EMsgGCCStrike15_v2_Client2GCEconPreviewDataBlockResponse)
            {
                var resp = new ClientGCMsgProtobuf<CMsgGCCStrike15_v2_Client2GCEconPreviewDataBlockResponse>(callback.Message);

                float? paintWear = null;
                if (resp.Body.iteminfo != null)
                {
                    byte[] buffer = BitConverter.GetBytes(resp.Body.iteminfo.paintwear);
                    paintWear = BitConverter.ToSingle(buffer, 0);
                }
                if (_pendingRequestTcs != null)
                {
                    _pendingRequestTcs.TrySetResult(paintWear);
                    _pendingRequestTcs = null;
                }
            }
        }

        /// <summary>
        /// Connects to Steam, logs on, sends a "playing CS:GO" message, and waits for GC readiness.
        /// Also starts a background callback pump.
        /// </summary>
        public async Task ConnectAndLogOnAsync()
        {
            StartCallbackPump();

            _client.Connect();

            var connectDeadline = DateTime.UtcNow.AddSeconds(30);
            while (!_isConnected && DateTime.UtcNow < connectDeadline)
            {
                await Task.Delay(200);
            }
            if (!_isConnected)
            {
                throw new Exception("Failed to connect to Steam within 30s.");
            }

            _user.LogOn(new SteamUser.LogOnDetails
            {
                Username = _username,
                Password = _password
            });

            var logonDeadline = DateTime.UtcNow.AddSeconds(30);
            while (!_isLoggedOn && DateTime.UtcNow < logonDeadline)
            {
                await Task.Delay(200);
            }
            if (!_isLoggedOn)
            {
                throw new Exception("Failed to log on to Steam within 30s.");
            }

            var playGame = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayed);
            playGame.Body.games_played.Add(new CMsgClientGamesPlayed.GamePlayed { game_id = APPID_CSGO });
            _client.Send(playGame);

            var gcDeadline = DateTime.UtcNow.AddSeconds(60);
            while (!_gcReady && DateTime.UtcNow < gcDeadline)
            {
                await Task.Delay(250);
            }
            if (!_gcReady)
            {
                throw new Exception("CS:GO GC never became ready.");
            }

            Console.WriteLine("Connected, logged on, and GC is ready.");
        }

        /// <summary>
        /// Requests an item's float/wear value.
        /// Because the GC response does not echo the request parameters, only one request
        /// is allowed at a time.
        /// </summary>
        public async Task<float?> GetFloatValueAsync(
    ulong paramS, ulong paramA, ulong paramD, ulong paramM,
    int timeoutSeconds = 10)
        {
            Console.WriteLine($"Calling {nameof(GetFloatValueAsync)}");
            await _floatRequestSemaphore.WaitAsync();

            try
            {
                if (!_gcReady)
                {
                    Console.WriteLine("Cannot request float: GC is not ready.");
                    return null;
                }
                if (_pendingRequestTcs != null)
                {
                    throw new InvalidOperationException("Another float request is in progress.");
                }

                var tcs = new TaskCompletionSource<float?>();
                _pendingRequestTcs = tcs;

                var request = new ClientGCMsgProtobuf<CMsgGCCStrike15_v2_Client2GCEconPreviewDataBlockRequest>(
                    (uint)ECsgoGCMsg.k_EMsgGCCStrike15_v2_Client2GCEconPreviewDataBlockRequest
                );
                request.Body.param_s = paramS;
                request.Body.param_a = paramA;
                request.Body.param_d = paramD;
                request.Body.param_m = paramM;

                Console.WriteLine($"Sending float request: s[{paramS}], a[{paramA}], d[{paramD}], m[{paramM}]");
                _gc.Send(request, APPID_CSGO);

                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));
                var completed = await Task.WhenAny(tcs.Task, timeoutTask);

                if (completed == timeoutTask)
                {
                    Console.WriteLine($"Float request timed out after {timeoutSeconds}s");
                    _pendingRequestTcs = null;
                    return null;
                }

                return tcs.Task.Result;
            }
            finally
            {
                _pendingRequestTcs = null;
                _floatRequestSemaphore.Release();
                Console.WriteLine($"Exiting {nameof(GetFloatValueAsync)}");
            }
        }

        /// <summary>
        /// Starts a background thread that pumps callbacks every 100ms.
        /// </summary>
        private void StartCallbackPump()
        {
            if (_callbackPumpThread != null) return;

            _runCallbackPump = true;
            _callbackPumpThread = new Thread(() =>
            {
                while (_runCallbackPump)
                {
                    _callbackManager.RunWaitCallbacks(TimeSpan.FromMilliseconds(300));
                }
            })
            {
                IsBackground = true,
                Name = "SteamFloatFetcher-CallbackPump"
            };
            _callbackPumpThread.Start();
        }

        /// <summary>
        /// Gracefully shuts down by stopping the callback pump and disconnecting.
        /// </summary>
        public void Disconnect()
        {
            _runCallbackPump = false;
            _callbackPumpThread?.Join(2000);
            _callbackPumpThread = null;

            var exitMsg = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayed);
            exitMsg.Body.games_played.Clear();
            _client.Send(exitMsg);
            _client.Disconnect();
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
