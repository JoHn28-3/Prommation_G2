#nullable enable

using System;
using System.Drawing;

namespace WHATSAPP_CHAPT.App.Services
{
    /// <summary>
    /// Fournisseur de photos de profil pour les utilisateurs et conversations.
    /// </summary>
    public interface IAvatarProvider
    {
        /// <summary>
        /// Obtient l'identifiant de contenu (hash SHA-256) de la photo associée à un propriétaire.
        /// </summary>
        /// <param name="ownerId">Identifiant du propriétaire.</param>
        /// <returns>Hash de la photo ou null si aucune photo n'est associée.</returns>
        string? GetPhotoId(Guid ownerId);

        /// <summary>
        /// Ouvre une nouvelle instance d'image pour un identifiant de photo donné.
        /// </summary>
        /// <param name="photoId">Identifiant de contenu de la photo.</param>
        /// <returns>Nouvelle instance d'image à libérer par l'appelant, ou null si introuvable.</returns>
        Image? OpenPhoto(string photoId);

        /// <summary>
        /// Définit ou met à jour la photo d'un propriétaire à partir d'un fichier source.
        /// </summary>
        /// <param name="ownerId">Identifiant du propriétaire.</param>
        /// <param name="sourceFilePath">Chemin du fichier image source.</param>
        /// <returns>True si l'enregistrement a réussi, sinon false.</returns>
        bool SetPhoto(Guid ownerId, string sourceFilePath);

        /// <summary>
        /// Supprime l'association de photo pour un propriétaire donné.
        /// </summary>
        /// <param name="ownerId">Identifiant du propriétaire.</param>
        void RemovePhoto(Guid ownerId);

        /// <summary>
        /// Événement levé lors de la modification ou suppression de la photo d'un propriétaire.
        /// </summary>
        event EventHandler<Guid>? PhotoChanged;
    }
}
