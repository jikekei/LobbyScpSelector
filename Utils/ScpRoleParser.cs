using System;
using System.Collections.Generic;
using PlayerRoles;

namespace LobbyScpSelector.Utils
{
    /// <summary>
    /// 可自选 SCP 角色定义与名称解析。
    /// </summary>
    internal static class ScpRoleParser
    {
        internal static readonly RoleTypeId[] SelectableRoles =
        {
            RoleTypeId.Scp173,
            RoleTypeId.Scp049,
            RoleTypeId.Scp096,
            RoleTypeId.Scp079,
            RoleTypeId.Scp3114,
            RoleTypeId.Scp106,
            RoleTypeId.Scp939
        };

        internal static readonly HashSet<RoleTypeId> SelectableRoleSet = new HashSet<RoleTypeId>(SelectableRoles);

        private static readonly Dictionary<string, RoleTypeId> Aliases =
            new Dictionary<string, RoleTypeId>(StringComparer.OrdinalIgnoreCase)
            {
                { "173", RoleTypeId.Scp173 },
                { "scp173", RoleTypeId.Scp173 },
                { "049", RoleTypeId.Scp049 },
                { "49", RoleTypeId.Scp049 },
                { "scp049", RoleTypeId.Scp049 },
                { "scp49", RoleTypeId.Scp049 },
                { "096", RoleTypeId.Scp096 },
                { "96", RoleTypeId.Scp096 },
                { "scp096", RoleTypeId.Scp096 },
                { "scp96", RoleTypeId.Scp096 },
                { "079", RoleTypeId.Scp079 },
                { "79", RoleTypeId.Scp079 },
                { "scp079", RoleTypeId.Scp079 },
                { "scp79", RoleTypeId.Scp079 },
                { "3114", RoleTypeId.Scp3114 },
                { "scp3114", RoleTypeId.Scp3114 },
                { "106", RoleTypeId.Scp106 },
                { "scp106", RoleTypeId.Scp106 },
                { "939", RoleTypeId.Scp939 },
                { "scp939", RoleTypeId.Scp939 }
            };

        internal static bool TryParse(string input, out RoleTypeId role)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                role = RoleTypeId.None;
                return false;
            }

            string normalized = input.Trim().Replace("-", string.Empty).Replace("_", string.Empty);
            return Aliases.TryGetValue(normalized, out role);
        }

        internal static string SupportedNames => "173, 049, 096, 079, 3114, 106, 939";

        internal static string FormatDisplayName(RoleTypeId role)
        {
            switch (role)
            {
                case RoleTypeId.Scp173:
                    return "Scp-173";
                case RoleTypeId.Scp049:
                    return "Scp-049";
                case RoleTypeId.Scp096:
                    return "Scp-096";
                case RoleTypeId.Scp079:
                    return "Scp-079";
                case RoleTypeId.Scp3114:
                    return "Scp-3114";
                case RoleTypeId.Scp106:
                    return "Scp-106";
                case RoleTypeId.Scp939:
                    return "Scp-939";
                default:
                    return role.ToString();
            }
        }
    }
}
