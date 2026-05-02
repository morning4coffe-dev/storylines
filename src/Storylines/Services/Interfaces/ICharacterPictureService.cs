using Storylines.Models;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace Storylines.Services.Interfaces
{
    /// <summary>
    /// Owns character profile-picture I/O so view-models do not need to reference
    /// <see cref="BitmapImage"/> or <see cref="StorageFile"/> directly. Keeping image work behind
    /// this seam is a pre-requisite for the WinUI 3 / multi-platform port where the imaging
    /// stack differs.
    /// </summary>
    public interface ICharacterPictureService
    {
        /// <summary>
        /// Load and decode a profile picture for binding. Returns <c>null</c> when the source is
        /// missing or fails to decode.
        /// </summary>
        Task<BitmapImage> LoadAsync(CharacterPicture picture);

        /// <summary>
        /// Persist a chosen <paramref name="source"/> into the project's character-picture store
        /// and return the descriptor to attach to the <see cref="Character"/> model.
        /// </summary>
        Task<CharacterPicture> ImportAsync(StorageFile source);

        /// <summary>
        /// Remove any on-disk asset associated with <paramref name="picture"/>. Safe to call when
        /// no asset exists.
        /// </summary>
        Task DeleteAsync(CharacterPicture picture);
    }
}
