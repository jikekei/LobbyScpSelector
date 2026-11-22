using System.Globalization;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace LobbyScpSelector.Utils
{
    /// <summary>
    /// 坐标校验、解析与格式化辅助工具。
    /// </summary>
    internal static class CoordinateHelper
    {
        internal static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        internal static bool IsFinite(Vector3 position) =>
            IsFinite(position.x) && IsFinite(position.y) && IsFinite(position.z);

        internal static bool TryParse(string value, out float coordinate) =>
            float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out coordinate) &&
            IsFinite(coordinate);

        internal static bool TryParseVector3(string xText, string yText, string zText, out Vector3 position)
        {
            if (TryParse(xText, out float x) &&
                TryParse(yText, out float y) &&
                TryParse(zText, out float z))
            {
                position = new Vector3(x, y, z);
                return true;
            }

            position = default;
            return false;
        }

        internal static Vector3 GetPlayerFeetPosition(Player player, out bool hitGround)
        {
            if (player == null || player.IsDestroyed || !IsFinite(player.Position))
            {
                hitGround = false;
                return Vector3.zero;
            }

            Transform playerTransform = player.GameObject != null ? player.GameObject.transform : null;
            RaycastHit[] hits = Physics.RaycastAll(player.Position, Vector3.down, 5f);

            float closestDistance = float.MaxValue;
            Vector3 closestPoint = default;
            bool found = false;

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || hit.collider.isTrigger)
                    continue;

                if (playerTransform != null && hit.transform.IsChildOf(playerTransform))
                    continue;

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    closestPoint = hit.point;
                    found = true;
                }
            }

            if (found)
            {
                hitGround = true;
                return closestPoint + new Vector3(0f, 0.05f, 0f);
            }

            hitGround = false;
            return player.Position + new Vector3(0f, -0.95f, 0f);
        }

        internal static string Format(Vector3 position) =>
            string.Format(CultureInfo.InvariantCulture, "{0:R} {1:R} {2:R}", position.x, position.y, position.z);
    }
}
