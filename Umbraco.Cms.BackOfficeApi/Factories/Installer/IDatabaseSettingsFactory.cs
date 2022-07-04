﻿using Umbraco.Cms.BackOfficeApi.Models.Installer;

namespace Umbraco.Cms.BackOfficeApi.Factories.Installer;

public interface IDatabaseSettingsFactory
{
    ICollection<DatabaseSettingsModel> GetDatabaseSettings();
}
