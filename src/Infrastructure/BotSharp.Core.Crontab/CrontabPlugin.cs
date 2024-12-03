/*****************************************************************************
  Copyright 2024 Written by Haiping Chen. All Rights Reserved.

  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at

      http://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.
******************************************************************************/

namespace BotSharp.Core.Crontab;

/// <summary>
///  Crontab plugin is a time-based job scheduler in agent framework. 
///  The cron system is used for automating repetitive tasks, such as trigger AI Agent to do specific task periodically.
/// </summary>
public class CrontabPlugin : IBotSharpPlugin
{
    public string Id => "3155c15e-28d3-43f7-8ead-fc43324ec21a";
    public string Name => "BotSharp Crontab";
    public string Description => "Crontab plugin is a time-based job scheduler in agent framework. The cron system is used to trigger AI Agent to do specific task periodically.";
    public string IconUrl => "https://icon-library.com/images/stop-watch-icon/stop-watch-icon-10.jpg";

    public string[] AgentIds =
    [
        BuiltInAgentId.Crontab
    ];

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<ICrontabService, CrontabService>();
        services.AddHostedService<CrontabWatcher>();
    }
}
