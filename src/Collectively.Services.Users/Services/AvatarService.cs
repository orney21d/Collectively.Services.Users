using System;
using System.Linq;
using System.Threading.Tasks;
using Collectively.Common.Domain;
using Collectively.Common.Files;
using Collectively.Common.Types;
using Collectively.Services.Users.Domain;
using Collectively.Services.Users.Repositories;
using Serilog;

namespace Collectively.Services.Users.Services
{
    public class AvatarService : IAvatarService
    {
        private static readonly ILogger Logger = Log.Logger;
        private readonly IUserRepository _userRepository;
        private readonly IFileHandler _fileHandler;
        private readonly IImageService _imageService;
        private readonly IFileValidator _fileValidator;

        public AvatarService(IUserRepository userRepository, 
            IFileHandler fileHandler, IImageService imageService,
            IFileValidator fileValidator)
        {
            _userRepository = userRepository;
            _fileHandler = fileHandler;
            _imageService = imageService;
            _fileValidator = fileValidator;
        }

        public async Task<string> GetUrlAsync(string userId)
        {
            var user = await _userRepository.GetByUserIdAsync(userId);
            if (user.HasNoValue)
            {
                throw new ServiceException(OperationCodes.UserNotFound,
                    $"User with id: '{userId}' has not been found.");
            }

            return user.Value.Avatar?.Url ?? string.Empty;
        }

        public async Task AddOrUpdateAsync(string userId, File avatar)
        {
            if (avatar == null)
            {
                throw new ServiceException(OperationCodes.InvalidFile, 
                    $"There is no avatar file to be uploaded.");
            }
            if(!_fileValidator.IsImage(avatar))
            {
                throw new ServiceException(OperationCodes.InvalidFile);
            }
            var user = await _userRepository.GetByUserIdAsync(userId);
            var name = $"avatar_{userId:N}.jpg";
            var resizedAvatar = _imageService.ProcessImage(avatar, 200);
            await RemoveAsync(user, userId);
            await _fileHandler.UploadAsync(resizedAvatar, name, (baseUrl, fullUrl) => {
                user.Value.SetAvatar(Avatar.Create(name, fullUrl));
            });
            await _userRepository.UpdateAsync(user.Value);
        }

        public async Task RemoveAsync(string userId)
        {
            var user = await _userRepository.GetByUserIdAsync(userId);
            await RemoveAsync(user, userId);
            await _userRepository.UpdateAsync(user.Value);
        }

        private async Task RemoveAsync(Maybe<User> user, string userId)
        {
            if (user.HasNoValue)
            {
                throw new ServiceException(OperationCodes.UserNotFound,
                    $"User with id: '{userId}' has not been found.");
            }
            if(user.Value.Avatar == null)
            {
                return;
            }
            if(user.Value.Avatar.IsEmpty)
            {
                return;
            }
            await _fileHandler.DeleteAsync(user.Value.Avatar.Name);
            user.Value.RemoveAvatar();
        }
    }
}