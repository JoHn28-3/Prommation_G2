#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WHATSAPP_CHAPT.Core.Models;
using WHATSAPP_CHAPT.Core.Services;
using WHATSAPP_CHAPT.Core.Rules;
using WHATSAPP_CHAPT.App.Services;
using WHATSAPP_CHAPT.Core.Events;

namespace WHATSAPP_CHAPT.App.SelfTest
{
    /// <summary>
    /// Autotests pour la phase 7C (Groupes logique).
    /// </summary>
    public static class Phase7CTests
    {
        public static void Run(List<(string Name, bool Success, string? Detail)> results)
        {
            // Helper to build a simple contact list
            IReadOnlyList<Guid> SampleMembers()
            {
                // Deux utilisateurs fictifs (pas besoin d'exister dans le service)
                return new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            }

            // ---------- GroupRules tests ----------
            Test("GroupRules_CreationValide", () =>
            {
                var service = new DemoChatService();
                var members = SampleMembers();
                var conv = service.CreateGroupAsync("Groupe Test", members).GetAwaiter().GetResult();
                if (conv == null) throw new Exception("Conversation null");
                if (conv.Title != "Groupe Test") throw new Exception("Titre incorrect");
                if (conv.MemberIds.Count != members.Count + 1) // +1 for CurrentUser
                    throw new Exception($"Mauvais nombre de membres {conv.MemberIds.Count}");
            }, results);

            Test("GroupRules_CreationTitreVide", () =>
            {
                var service = new DemoChatService();
                var members = SampleMembers();
                try
                {
                    service.CreateGroupAsync(string.Empty, members).GetAwaiter().GetResult();
                    throw new Exception("Création acceptée avec titre vide");
                }
                catch (ArgumentException) { /* attendu */ }
            }, results);

            Test("GroupRules_CreationMoinsDe2Membres", () =>
            {
                var service = new DemoChatService();
                var members = new List<Guid>(); // aucun membre supplémentaire
                try
                {
                    service.CreateGroupAsync("Groupe", members).GetAwaiter().GetResult();
                    throw new Exception("Création acceptée avec moins de 2 membres");
                }
                catch (InvalidOperationException) { /* attendu */ }
            }, results);

            Test("GroupRules_CreationDoublonMembre", () =>
            {
                var service = new DemoChatService();
                var member = Guid.NewGuid();
                var members = new List<Guid> { member, member }; // doublon
                try
                {
                    service.CreateGroupAsync("Groupe", members).GetAwaiter().GetResult();
                    throw new Exception("Création acceptée avec doublon");
                }
                catch (ArgumentException) { /* attendu */ }
            }, results);

            Test("GroupRules_AjoutParAdmin", () =>
            {
                var service = new DemoChatService();
                var members = SampleMembers();
                var conv = service.CreateGroupAsync("Groupe", members).GetAwaiter().GetResult();
                var newMember = Guid.NewGuid();
                // CurrentUser est Administrateur dans DemoChatService
                var ok = service.AddMemberAsync(conv.Id, newMember).GetAwaiter().GetResult();
                if (!ok) throw new Exception("AddMemberAsync a échoué pour admin");
                // Vérifier présence
                var refreshed = service.GetConversationAsync(conv.Id).GetAwaiter().GetResult();
                if (refreshed == null || !refreshed.MemberIds.Contains(newMember)) throw new Exception("Membre non ajouté");
            }, results);

            Test("GroupRules_AjoutParMembreSimpleRefuse", () =>
            {
                var service = new DemoChatService();
                // Simuler un utilisateur simple en changeant CurrentUser du service
                var simpleUser = new UserIdentity { Id = Guid.NewGuid(), DisplayName = "Simple" };
                service.SetCurrentUser(simpleUser); // méthode fictive, on l'ajoute ci‑dessous
                var members = SampleMembers();
                var conv = service.CreateGroupAsync("Groupe", members).GetAwaiter().GetResult();
                var newMember = Guid.NewGuid();
                var ok = service.AddMemberAsync(conv.Id, newMember).GetAwaiter().GetResult();
                if (ok) throw new Exception("AddMemberAsync a accepté un simple membre");
            }, results);

            Test("GroupRules_AjoutMembreDejaPresentRefuse", () =>
            {
                var service = new DemoChatService();
                var members = SampleMembers();
                var conv = service.CreateGroupAsync("Groupe", members).GetAwaiter().GetResult();
                var existing = conv.MemberIds.First();
                var ok = service.AddMemberAsync(conv.Id, existing).GetAwaiter().GetResult();
                if (ok) throw new Exception("AddMemberAsync a accepté membre déjà présent");
            }, results);

            Test("GroupRules_RetraitParAdmin", () =>
            {
                var service = new DemoChatService();
                var members = SampleMembers();
                var conv = service.CreateGroupAsync("Groupe", members).GetAwaiter().GetResult();
                var target = conv.MemberIds.First(id => id != service.CurrentUser.Id);
                var ok = service.RemoveMemberAsync(conv.Id, target).GetAwaiter().GetResult();
                if (!ok) throw new Exception("RemoveMemberAsync a échoué pour admin");
                var refreshed = service.GetConversationAsync(conv.Id).GetAwaiter().GetResult();
                if (refreshed != null && refreshed.MemberIds.Contains(target)) throw new Exception("Membre toujours présent après retrait");
            }, results);

            Test("GroupRules_RetraitAdminRefuse", () =>
            {
                var service = new DemoChatService();
                var members = SampleMembers();
                var conv = service.CreateGroupAsync("Groupe", members).GetAwaiter().GetResult();
                // Admin = CurrentUser ; tenter de le retirer
                var ok = service.RemoveMemberAsync(conv.Id, service.CurrentUser.Id).GetAwaiter().GetResult();
                if (ok) throw new Exception("RemoveMemberAsync a autorisé le retrait de l'admin");
            }, results);

            Test("GroupRules_AutoRetraitRefuse", () =>
            {
                var service = new DemoChatService();
                var members = SampleMembers();
                var conv = service.CreateGroupAsync("Groupe", members).GetAwaiter().GetResult();
                // Simuler remise du service en mode simple (pas admin)
                var simpleUser = new UserIdentity { Id = Guid.NewGuid(), DisplayName = "Simple" };
                service.SetCurrentUser(simpleUser);
                var ok = service.LeaveGroupAsync(conv.Id).GetAwaiter().GetResult();
                // Dans DemoChatService, LeaveGroupAsync n'est autorisé que pour admin ?
                // On attend false pour un simple membre (selon spécifications)
                if (ok) throw new Exception("LeaveGroupAsync a accepté pour simple membre");
            }, results);

            // ---------- DemoChatService group workflow ----------
            Test("DemoChatService_CreationGroupeVisible", () =>
            {
                var service = new DemoChatService();
                var members = SampleMembers();
                var conv = service.CreateGroupAsync("Groupe Visible", members).GetAwaiter().GetResult();
                var all = service.GetConversationsAsync().GetAwaiter().GetResult();
                if (!all.Any(c => c.Id == conv.Id)) throw new Exception("Conversation non visible dans GetConversationsAsync");
                // Vérifier que l'événement ConversationUpdated est émis
                bool eventRaised = false;
                service.ChatEventRaised += (s, e) =>
                {
                    if (e.Type == ChatEventType.ConversationUpdated && e.ConversationId == conv.Id)
                        eventRaised = true;
                };
                // Recréer pour déclencher l'événement (déjà déclenché dans CreateGroupAsync)
                // On se contente de vérifier l'événement déjà levé
                if (!eventRaised) throw new Exception("ConversationUpdated non émis lors de la création");
            }, results);

            Test("DemoChatService_AjoutMembre", () =>
            {
                var service = new DemoChatService();
                var members = SampleMembers();
                var conv = service.CreateGroupAsync("Test Ajout", members).GetAwaiter().GetResult();
                var newMember = Guid.NewGuid();
                var ok = service.AddMemberAsync(conv.Id, newMember).GetAwaiter().GetResult();
                if (!ok) throw new Exception("AddMemberAsync a échoué");
                var refreshed = service.GetConversationAsync(conv.Id).GetAwaiter().GetResult();
                if (refreshed == null || !refreshed.MemberIds.Contains(newMember)) throw new Exception("Membre ajouté absent");
            }, results);

            Test("DemoChatService_RetraitMembre", () =>
            {
                var service = new DemoChatService();
                var members = SampleMembers();
                var conv = service.CreateGroupAsync("Test Retrait", members).GetAwaiter().GetResult();
                var target = conv.MemberIds.First(id => id != service.CurrentUser.Id);
                var ok = service.RemoveMemberAsync(conv.Id, target).GetAwaiter().GetResult();
                if (!ok) throw new Exception("RemoveMemberAsync a échoué");
                var refreshed = service.GetConversationAsync(conv.Id).GetAwaiter().GetResult();
                if (refreshed != null && refreshed.MemberIds.Contains(target)) throw new Exception("Membre toujours présent après retrait");
            }, results);

            Test("DemoChatService_DepartMembre", () =>
            {
                var service = new DemoChatService();
                var members = SampleMembers();
                var conv = service.CreateGroupAsync("Test Départ", members).GetAwaiter().GetResult();
                var ok = service.LeaveGroupAsync(conv.Id).GetAwaiter().GetResult();
                if (!ok) throw new Exception("LeaveGroupAsync a échoué");
                var exists = service.GetConversationAsync(conv.Id).GetAwaiter().GetResult();
                if (exists != null) throw new Exception("Conversation toujours existante après dernier membre quitté");
            }, results);
        }

        private static void Test(string name, Action testBody, List<(string Name, bool Success, string? Detail)> results)
        {
            try
            {
                testBody();
                results.Add((name, true, null));
            }
            catch (Exception ex)
            {
                results.Add((name, false, ex.Message));
            }
        }
    }
}
