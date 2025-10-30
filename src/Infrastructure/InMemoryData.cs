using RippleSync.Application.Integrations;
using RippleSync.Domain.Platforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RippleSync.Infrastructure;
internal class InMemoryData
{
    internal static readonly List<IntegrationResponse> IntegrationResponses =
    [
        new ((int)Platforms.X, "X.com", "Share updates on X", false, ""),
        new ((int)Platforms.LinkedIn, "LinkedIn", "Share professional updates on LinkedIn", true, ""),
        new ((int)Platforms.Facebook, "Facebook", "Create posts on Facebook", false, ""),
        new ((int)Platforms.Instagram, "Instagram", "Post photos and stories on Instagram", false, ""),
        new ((int)Platforms.Threads, "Threads", "Post photos and stories on Instagram", false, "")
    ];
}
