using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RippleSync.Infrastructure.UserPlatformIntegrationRepository;
internal class PostgresIntegrationRepository
{
    public async Task GetUserIntegrations(Guid userId)
    {
        string userIntegrationQuery =
@"SELECT 
    p.id,
    p.platform_name AS name,
    CASE WHEN upi.id IS NOT NULL THEN true ELSE false END AS connected,
    p.platform_description AS description,
    p.image_url AS ""imageUrl""
FROM platform p
LEFT JOIN user_platform_integration upi 
    ON p.id = upi.platform_id 
    AND upi.user_account_id = @userId
ORDER BY p.platform_name;";
    }
}
