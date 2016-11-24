﻿using System;
using System.Threading.Tasks;
using Coolector.Common.Domain;
using Coolector.Common.Encryption;
using Coolector.Services.Users.Domain;
using Coolector.Services.Users.Repositories;

namespace Coolector.Services.Users.Services
{
    public class PasswordService : IPasswordService
    {
        private readonly IUserRepository _userRepository;
        private readonly IOneTimeSecuredOperationService _oneTimeSecuredOperationService;
        private readonly IEncrypter _encrypter;

        public PasswordService(IUserRepository userRepository,
            IOneTimeSecuredOperationService oneTimeSecuredOperationService,
            IEncrypter encrypter)
        {
            _userRepository = userRepository;
            _oneTimeSecuredOperationService = oneTimeSecuredOperationService;
            _encrypter = encrypter;
        }

        public async Task ChangeAsync(string userId, string currentPassword, string newPassword)
        {
            var user = await _userRepository.GetByUserIdAsync(userId);
            if (user.HasNoValue)
                throw new ServiceException($"User with id: '{userId}' has not been found.");
            if (user.Value.Provider != Providers.Coolector)
                throw new ServiceException($"Password can not be changed for the account type: ;{user.Value.Provider}'.");
            if (!user.Value.ValidatePassword(currentPassword, _encrypter))
                throw new ServiceException("Current password is invalid.");

            user.Value.SetPassword(newPassword, _encrypter);
            await _userRepository.UpdateAsync(user.Value);
        }

        public async Task ResetAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email, Providers.Coolector);
            if (user.HasNoValue)
                throw new ServiceException($"User with email: '{email}' has not been found.");

            var operationId = Guid.NewGuid();
            await _oneTimeSecuredOperationService.CreateAsync(operationId, OneTimeSecuredOperations.ResetPassword,
                email, DateTime.UtcNow.AddDays(1));

            //TODO: Send email message;
        }

        public async Task SetNewAsync(string email, string token, string password)
        {
            var user = await _userRepository.GetByEmailAsync(email, Providers.Coolector);
            if (user.HasNoValue)
                throw new ServiceException($"User with email: '{email}' has not been found.");

            await _oneTimeSecuredOperationService.ConsumeAsync(OneTimeSecuredOperations.ResetPassword,
                email, token);
            user.Value.SetPassword(password, _encrypter);
            await _userRepository.UpdateAsync(user.Value);
        }
    }
}