#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WHATSAPP_CHAPT.App.Forms;
using WHATSAPP_CHAPT.App.Services;

namespace WHATSAPP_CHAPT.App.SelfTest
{
    /// <summary>
    /// Autotests pour la phase H (Signalisation des appels audio et vidéo P2P).
    /// </summary>
    public static class Phase7HTests
    {
        public static void Run(List<(string Name, bool Success, string? Detail)> results)
        {
            Test("CallService_IpVideDeclencheArgumentException", () =>
            {
                var mockConn = new MockConnectionService();
                var callService = new CallService(mockConn);
                try
                {
                    callService.InitiateCallAsync("   ").GetAwaiter().GetResult();
                    throw new Exception("Aurait dû lever ArgumentException");
                }
                catch (ArgumentException)
                {
                    // Comportement attendu
                }
            }, results);

            Test("CallService_AnswerCallSansAppelEnAttente_RenvoieFalse", () =>
            {
                var mockConn = new MockConnectionService();
                var callService = new CallService(mockConn);
                bool answered = callService.AnswerCallAsync(true).GetAwaiter().GetResult();
                if (answered) throw new Exception("AnswerCall sans appel en attente ne doit pas réussir");
            }, results);

            Test("CallType_EnumValide", () =>
            {
                var audio = CallType.Audio;
                var video = CallType.Video;
                if (audio == video) throw new Exception("CallType Audio et Video doivent être distincts");
            }, results);
        }

        private sealed class MockConnectionService : IConnectionService
        {
            public Task<(bool Success, string RemoteId)> ConnectAsync(string ip, int port = 5000)
            {
                return Task.FromResult((true, Guid.NewGuid().ToString("D")));
            }

            public Task<Guid> GetLocalIdAsync()
            {
                return Task.FromResult(Guid.NewGuid());
            }

            public Task StartBroadcastListenerAsync() => Task.CompletedTask;

            public event EventHandler<(string Ip, string RemoteId)>? Discovered
            {
                add { }
                remove { }
            }
        }

        private static void Test(string name, Action body, List<(string Name, bool Success, string? Detail)> results)
        {
            try
            {
                body();
                results.Add((name, true, null));
            }
            catch (Exception ex)
            {
                results.Add((name, false, ex.Message));
            }
        }
    }
}
