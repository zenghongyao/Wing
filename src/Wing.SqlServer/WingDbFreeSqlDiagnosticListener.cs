﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Wing.APM.FreeSql;

namespace Wing.Persistence
{
    public class WingDbFreeSqlDiagnosticListener : FreeSqlDiagnosticListener
    {
        public override string Name => "WingDbFreeSqlDiagnosticListener";

        public WingDbFreeSqlDiagnosticListener(IHttpContextAccessor httpContextAccessor, ILogger<WingDbFreeSqlDiagnosticListener> logger)
            : base(httpContextAccessor, logger)
        {
        }
    }
}