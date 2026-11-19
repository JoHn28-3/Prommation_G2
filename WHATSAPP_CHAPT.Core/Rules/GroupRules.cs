#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using WHATSAPP_CHAPT.Core.Models;

namespace WHATSAPP_CHAPT.Core.Rules
{
    /// <summary>
    /// Règles métier pour la gestion des groupes et de leurs membres.
    /// </summary>
    public static class GroupRules
    {
        /// <summary>
        /// Valide les paramètres pour la création d'un nouveau groupe.
        /// </summary>
        /// <param name="title">Titre du groupe.</param>
        /// <param name="creatorId">Identifiant du créateur.</param>
        /// <param name="memberIds">Liste des membres supplémentaires.</param>
        /// <returns>True si les paramètres sont valides ; sinon false.</returns>
        public static bool ValidateNewGroup(string? title, Guid creatorId, IReadOnlyList<Guid>? memberIds)
        {
            return ValidateNewGroup(title, creatorId, memberIds, out _);
        }

        /// <summary>
        /// Valide les paramètres pour la création d'un nouveau groupe avec message d'erreur.
        /// </summary>
        /// <param name="title">Titre du groupe.</param>
        /// <param name="creatorId">Identifiant du créateur.</param>
        /// <param name="memberIds">Liste des membres supplémentaires.</param>
        /// <param name="errorMessage">Message d'erreur descriptif si invalide.</param>
        /// <returns>True si les paramètres sont valides ; sinon false.</returns>
        public static bool ValidateNewGroup(string? title, Guid creatorId, IReadOnlyList<Guid>? memberIds, out string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                errorMessage = "Le titre du groupe ne peut pas être vide.";
                return false;
            }

            if (memberIds == null || memberIds.Count < 2)
            {
                errorMessage = "Un groupe doit comporter au moins 2 membres en plus du créateur.";
                return false;
            }

            if (creatorId != Guid.Empty && memberIds.Contains(creatorId))
            {
                errorMessage = "Le créateur ne doit pas être inclus dans la liste des membres à ajouter.";
                return false;
            }

            if (memberIds.Distinct().Count() != memberIds.Count)
            {
                errorMessage = "La liste des membres contient des doublons.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        /// <summary>
        /// Détermine si un utilisateur peut ajouter un membre au groupe.
        /// </summary>
        /// <param name="conversation">Conversation ciblée.</param>
        /// <param name="actorUserId">Identifiant de l'utilisateur qui effectue l'action.</param>
        /// <param name="targetUserId">Identifiant du membre à ajouter.</param>
        /// <returns>True si l'ajout est permis ; sinon false.</returns>
        public static bool CanAddMember(Conversation? conversation, Guid actorUserId, Guid targetUserId)
        {
            if (conversation == null || conversation.Type != ConversationType.Group)
            {
                return false;
            }

            if (actorUserId == Guid.Empty || targetUserId == Guid.Empty)
            {
                return false;
            }

            var actor = conversation.Members.FirstOrDefault(m => m.UserId == actorUserId);
            if (actor == null || actor.Role != MemberRole.Administrator)
            {
                return false;
            }

            if (conversation.Members.Any(m => m.UserId == targetUserId))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Détermine si un utilisateur peut retirer un membre du groupe.
        /// </summary>
        /// <param name="conversation">Conversation ciblée.</param>
        /// <param name="actorUserId">Identifiant de l'utilisateur qui effectue l'action.</param>
        /// <param name="targetUserId">Identifiant du membre à retirer.</param>
        /// <returns>True si le retrait est permis ; sinon false.</returns>
        public static bool CanRemoveMember(Conversation? conversation, Guid actorUserId, Guid targetUserId)
        {
            if (conversation == null || conversation.Type != ConversationType.Group)
            {
                return false;
            }

            if (actorUserId == Guid.Empty || targetUserId == Guid.Empty)
            {
                return false;
            }

            if (actorUserId == targetUserId)
            {
                return false;
            }

            var actor = conversation.Members.FirstOrDefault(m => m.UserId == actorUserId);
            if (actor == null || actor.Role != MemberRole.Administrator)
            {
                return false;
            }

            var target = conversation.Members.FirstOrDefault(m => m.UserId == targetUserId);
            if (target == null || target.Role == MemberRole.Administrator)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Détermine si un utilisateur peut modifier le rôle d'un membre.
        /// </summary>
        /// <param name="conversation">Conversation ciblée.</param>
        /// <param name="actorUserId">Identifiant de l'utilisateur qui effectue l'action.</param>
        /// <param name="targetUserId">Identifiant du membre cible.</param>
        /// <param name="newRole">Nouveau rôle souhaité.</param>
        /// <returns>True si le changement de rôle est permis ; sinon false.</returns>
        public static bool CanChangeRole(Conversation? conversation, Guid actorUserId, Guid targetUserId, MemberRole newRole)
        {
            if (conversation == null || conversation.Type != ConversationType.Group)
            {
                return false;
            }

            if (actorUserId == Guid.Empty || targetUserId == Guid.Empty)
            {
                return false;
            }

            var actor = conversation.Members.FirstOrDefault(m => m.UserId == actorUserId);
            if (actor == null || actor.Role != MemberRole.Administrator)
            {
                return false;
            }

            var target = conversation.Members.FirstOrDefault(m => m.UserId == targetUserId);
            if (target == null || target.Role == newRole)
            {
                return false;
            }

            if (target.Role == MemberRole.Administrator && newRole == MemberRole.Member)
            {
                int adminCount = conversation.Members.Count(m => m.Role == MemberRole.Administrator);
                if (adminCount <= 1)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Détermine si un utilisateur peut quitter le groupe.
        /// </summary>
        /// <param name="conversation">Conversation ciblée.</param>
        /// <param name="actorUserId">Identifiant de l'utilisateur souhaitant quitter.</param>
        /// <returns>True si l'utilisateur est membre du groupe ; sinon false.</returns>
        public static bool CanLeave(Conversation? conversation, Guid actorUserId)
        {
            if (conversation == null || conversation.Type != ConversationType.Group)
            {
                return false;
            }

            if (actorUserId == Guid.Empty)
            {
                return false;
            }

            return conversation.Members.Any(m => m.UserId == actorUserId);
        }

        /// <summary>
        /// Calcule la liste des membres restants après le départ d'un utilisateur selon les règles.
        /// </summary>
        /// <param name="conversation">Conversation d'origine.</param>
        /// <param name="actorUserId">Membre quittant le groupe.</param>
        /// <param name="shouldDelete">Indique si le groupe doit être supprimé car vide.</param>
        /// <returns>Liste des membres mis à jour.</returns>
        public static IReadOnlyList<Member> ComputeMembersAfterLeave(Conversation conversation, Guid actorUserId, out bool shouldDelete)
        {
            var remaining = conversation.Members
                .Where(m => m.UserId != actorUserId)
                .Select(m => new Member { UserId = m.UserId, Role = m.Role })
                .ToList();

            if (remaining.Count == 0)
            {
                shouldDelete = true;
                return Array.Empty<Member>();
            }

            shouldDelete = false;

            if (!remaining.Any(m => m.Role == MemberRole.Administrator))
            {
                var first = remaining[0];
                remaining[0] = new Member { UserId = first.UserId, Role = MemberRole.Administrator };
            }

            return remaining;
        }
    }
}
