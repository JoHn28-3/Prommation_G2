#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using WHATSAPP_CHAPT.Core.Models;

namespace WHATSAPP_CHAPT.App.Models
{
    /// <summary>
    /// Représente un membre affiché dans la fiche d'informations d'un groupe.
    /// </summary>
    public sealed class GroupMemberItem
    {
        public Guid UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public MemberRole Role { get; set; } = MemberRole.Member;
        public bool IsCurrentUser { get; set; }
        public string RoleDescription => Role == MemberRole.Administrator ? "Admin" : "Membre";
    }

    /// <summary>
    /// Modèle de vue pour afficher et gérer les détails d'un groupe (Phase D).
    /// </summary>
    public sealed class GroupInfoViewModel
    {
        public Guid ConversationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int MemberCount => Members.Count;
        public List<GroupMemberItem> Members { get; set; } = new();
        public bool IsCurrentUserAdmin { get; set; }

        public static GroupInfoViewModel FromConversation(
            Conversation conversation,
            Guid currentUserId,
            IReadOnlyDictionary<Guid, string> contactNames)
        {
            var vm = new GroupInfoViewModel
            {
                ConversationId = conversation.Id,
                Title = conversation.Title ?? "Groupe sans titre",
                IsCurrentUserAdmin = conversation.Members.Any(m => m.UserId == currentUserId && m.Role == MemberRole.Administrator)
            };

            foreach (var member in conversation.Members)
            {
                string name = member.UserId == currentUserId
                    ? "Vous"
                    : (contactNames.TryGetValue(member.UserId, out var n) ? n : "Membre inconnu");

                vm.Members.Add(new GroupMemberItem
                {
                    UserId = member.UserId,
                    DisplayName = name,
                    Role = member.Role,
                    IsCurrentUser = member.UserId == currentUserId
                });
            }

            return vm;
        }
    }
}
