﻿using System;
using System.Threading.Tasks;
using Collectively.Common.Domain;
using Collectively.Common.Services;
using Collectively.Common.Types;
using Collectively.Services.Users.Domain;
using Collectively.Services.Users.Repositories;


namespace Collectively.Services.Users.Services
{
    public class OneTimeSecuredOperationService : IOneTimeSecuredOperationService
    {
        private readonly IOneTimeSecuredOperationRepository _oneTimeSecuredOperationRepository;
        private readonly IEncrypter _encrypter;

        public OneTimeSecuredOperationService(IOneTimeSecuredOperationRepository oneTimeSecuredOperationRepository,
            IEncrypter encrypter)
        {
            _oneTimeSecuredOperationRepository = oneTimeSecuredOperationRepository;
            _encrypter = encrypter;
        }

        public async Task<Maybe<OneTimeSecuredOperation>> GetAsync(Guid id)
            => await _oneTimeSecuredOperationRepository.GetAsync(id);

        public async Task CreateAsync(Guid id, string type, string user, DateTime expiry)
        {
            var token = _encrypter.GetRandomSecureKey();
            var operation = new OneTimeSecuredOperation(id, type, user, token, expiry);
            await _oneTimeSecuredOperationRepository.AddAsync(operation);
        }

        public async Task<bool> CanBeConsumedAsync(string type,  string user, string token)
        {
            var operation = await _oneTimeSecuredOperationRepository
                .GetAsync(type, user, token);

            return operation.HasValue && operation.Value.CanBeConsumed();
        }

        public async Task ConsumeAsync(string type, string user, string token)
        {
            var operation = await _oneTimeSecuredOperationRepository
                .GetAsync(type, user, token);

            if (operation.HasNoValue)
            {
                throw new ServiceException(OperationCodes.OperationNotFound,
                    "Operation has not been found.");
            }

            operation.Value.Consume();
            await _oneTimeSecuredOperationRepository.UpdateAsync(operation.Value);
        }
    }
}