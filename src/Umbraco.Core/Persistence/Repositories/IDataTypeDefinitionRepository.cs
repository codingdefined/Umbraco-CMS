﻿using System.Collections.Generic;
using Umbraco.Core.Events;
using Umbraco.Core.Models;

namespace Umbraco.Core.Persistence.Repositories
{
    public interface IDataTypeDefinitionRepository : IReadWriteQueryRepository<int, IDataTypeDefinition>
    {

        PreValueCollection GetPreValuesCollectionByDataTypeId(int dataTypeId);
        string GetPreValueAsString(int preValueId);

        void AddOrUpdatePreValues(IDataTypeDefinition dataType, IDictionary<string, PreValue> values);
        void AddOrUpdatePreValues(int dataTypeId, IDictionary<string, PreValue> values);
        IEnumerable<MoveEventInfo<IDataTypeDefinition>> Move(IDataTypeDefinition toMove, EntityContainer container);
    }
}