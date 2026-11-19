#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using WHATSAPP_CHAPT.App.Models;
using WHATSAPP_CHAPT.App.Validation;
using WHATSAPP_CHAPT.Core.Models;

namespace WHATSAPP_CHAPT.App.SelfTest
{
    /// <summary>
    /// Autotests pour la phase D (Groupes - Interface et validation).
    /// </summary>
    public static class Phase7DTests
    {
        public static void Run(List<(string Name, bool Success, string? Detail)> results)
        {
            Test("NewGroupValidation_TitreValideEtMembresValides", () =>
            {
                var validation = NewGroupValidation.Validate("Projet C#", new List<string> { "Alice", "Bob" });
                if (!validation.IsValid) throw new Exception($"Échec inattendu : {validation.ErrorMessage}");
            }, results);

            Test("NewGroupValidation_TitreVideRefuse", () =>
            {
                var validation = NewGroupValidation.Validate("   ", new List<string> { "Alice", "Bob" });
                if (validation.IsValid) throw new Exception("Titre vide accepté à tort");
            }, results);

            Test("NewGroupValidation_MoinsDe2MembresRefuse", () =>
            {
                var validation = NewGroupValidation.Validate("Groupe", new List<string> { "Alice" });
                if (validation.IsValid) throw new Exception("Groupe avec un seul membre accepté à tort");
            }, results);

            Test("NewGroupValidation_DoublonMembresRefuse", () =>
            {
                var validation = NewGroupValidation.Validate("Groupe", new List<string> { "Alice", "Alice" });
                if (validation.IsValid) throw new Exception("Groupe avec doublons accepté à tort");
            }, results);

            Test("GroupInfoViewModel_FromConversation_ProjectionCorrecte", () =>
            {
                var currentUserId = Guid.NewGuid();
                var member2Id = Guid.NewGuid();
                var conv = new Conversation
                {
                    Id = Guid.NewGuid(),
                    Type = ConversationType.Group,
                    Title = "Team Dev",
                    Members = new List<Member>
                    {
                        new() { UserId = currentUserId, Role = MemberRole.Administrator },
                        new() { UserId = member2Id, Role = MemberRole.Member }
                    }
                };

                var contacts = new Dictionary<Guid, string>
                {
                    [member2Id] = "Bob Dev"
                };

                var vm = GroupInfoViewModel.FromConversation(conv, currentUserId, contacts);
                if (vm.Title != "Team Dev") throw new Exception($"Titre incorrect: {vm.Title}");
                if (vm.MemberCount != 2) throw new Exception($"Nombre membres incorrect: {vm.MemberCount}");
                if (!vm.IsCurrentUserAdmin) throw new Exception("L'utilisateur actuel devrait être admin");

                var currentUserItem = vm.Members.FirstOrDefault(m => m.IsCurrentUser);
                if (currentUserItem == null || currentUserItem.DisplayName != "Vous")
                    throw new Exception("L'item de l'utilisateur courant est invalide");

                var bobItem = vm.Members.FirstOrDefault(m => m.UserId == member2Id);
                if (bobItem == null || bobItem.DisplayName != "Bob Dev" || bobItem.Role != MemberRole.Member)
                    throw new Exception("L'item du contact est invalide");
            }, results);

            Test("GroupInfoViewModel_FromConversation_NonAdmin", () =>
            {
                var currentUserId = Guid.NewGuid();
                var adminId = Guid.NewGuid();
                var conv = new Conversation
                {
                    Id = Guid.NewGuid(),
                    Type = ConversationType.Group,
                    Title = "Club Sport",
                    Members = new List<Member>
                    {
                        new() { UserId = adminId, Role = MemberRole.Administrator },
                        new() { UserId = currentUserId, Role = MemberRole.Member }
                    }
                };

                var vm = GroupInfoViewModel.FromConversation(conv, currentUserId, new Dictionary<Guid, string>());
                if (vm.IsCurrentUserAdmin) throw new Exception("L'utilisateur actuel ne devrait pas être admin");
            }, results);
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
