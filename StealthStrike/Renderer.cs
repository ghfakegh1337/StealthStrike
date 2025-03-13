using System;
using System.Collections.Concurrent;
using System.Numerics;
using ClickableTransparentOverlay;
using TextCopy;
using ImGuiNET;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BasicESP
{
    public class Renderer : Overlay
    {
        public Vector2 screenSize = GetScreenSize();

        private ConcurrentQueue<Entity> entities = new ConcurrentQueue<Entity>();
        private Entity localPlayer = new Entity();
        private readonly object entityLock = new object();

        private bool enableESP = false;
        private bool enableTrigger = false;
        private Vector4 enemyColor = new Vector4(1, 0, 0, 1);
        private Vector4 teamColor = new Vector4(0, 0, 1, 1);

        ImDrawListPtr drawList;

        public bool EnableTrigger
        {
            get { return enableTrigger; }
            set { enableTrigger = value; }
        }

        protected override void Render()
        {
            unsafe
            {
                ImGuiIOPtr io = ImGui.GetIO();
                io.NativePtr->IniFilename = null;
            }

            ImGui.Begin("StealthStrike", ImGuiWindowFlags.NoSavedSettings);
            ImGui.Checkbox("Enable ESP", ref enableESP);

            if (ImGui.CollapsingHeader("Team color"))
                ImGui.ColorPicker4("##teamcolor", ref teamColor);

            if (ImGui.CollapsingHeader("Enemy color"))
                ImGui.ColorPicker4("##enemycolor", ref enemyColor);

            ImGui.Checkbox("Enable Trigger", ref enableTrigger);

            if (ImGui.Button("Copy My Discord"))
            {
                ClipboardService.SetText("ghfakegh1337");
            }

            if (ImGui.Button("Copy My Telegram"))
            {
                ClipboardService.SetText("@ghfakegh1337");
            }

            if (ImGui.Button("Open My GitHub"))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/ghfakegh1337/StealthStrike",
                    UseShellExecute = true
                });
            }

            DrawOverlay(screenSize);
            drawList = ImGui.GetWindowDrawList();

            if (enableESP)
            {
                foreach (var entity in entities)
                {
                    if (entity.position == Vector3.Zero) // Скипаем игрока который на нулевых кординатах (нужно для фикса чтобы ложно не ресовало)
                        continue;

                    if (entity.position == localPlayer.position) // Проверяем, является ли это локальный игрок
                        continue; // Пропускаем отрисовку

                    if (EntityOnScreen(entity))
                    {
                        DrawBox(entity);
                        DrawLine(entity);
                        DrawHealthBar(entity);
                        DrawDistance(entity); // Добавляем отрисовку расстояния
                    }
                }
            }

            ImGui.End();
        }

        bool EntityOnScreen(Entity entity)
        {
            return entity.position2D.X > 0 && entity.position2D.X < screenSize.X && entity.position2D.Y > 0 && entity.position2D.Y < screenSize.Y;
        }

        private void DrawBox(Entity entity)
        {
            float entityHeight = entity.position2D.Y - entity.viewPosition2D.Y;
            Vector2 rectTop = new Vector2(entity.viewPosition2D.X - entityHeight / 3, entity.viewPosition2D.Y);
            Vector2 rectBottom = new Vector2(entity.position2D.X + entityHeight / 3, entity.position2D.Y);
            Vector4 boxColor = localPlayer.team == entity.team ? teamColor : enemyColor;
            drawList.AddRect(rectTop, rectBottom, ImGui.ColorConvertFloat4ToU32(boxColor));
        }

        private void DrawLine(Entity entity)
        {
            Vector4 lineColor = localPlayer.team == entity.team ? teamColor : enemyColor;
            drawList.AddLine(new Vector2(screenSize.X / 2, screenSize.Y), entity.position2D, ImGui.ColorConvertFloat4ToU32(lineColor));
        }

        private void DrawHealthBar(Entity entity)
        {
            float entityHeight = entity.position2D.Y - entity.viewPosition2D.Y;
            float healthBarHeight = entityHeight;
            float healthBarWidth = 5.0f;
            float healthPercentage = Math.Clamp((float)entity.health / 100.0f, 0.0f, 1.0f);
            float filledHeight = healthBarHeight * healthPercentage;

            Vector2 healthBarTop = new Vector2(entity.viewPosition2D.X - entityHeight / 3 - healthBarWidth - 4, entity.viewPosition2D.Y);
            Vector2 healthBarBottom = new Vector2(healthBarTop.X + healthBarWidth, entity.viewPosition2D.Y + healthBarHeight);

            drawList.AddRectFilled(healthBarTop, healthBarBottom, ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 1)));
            drawList.AddRectFilled(new Vector2(healthBarTop.X, healthBarBottom.Y - filledHeight), new Vector2(healthBarBottom.X, healthBarBottom.Y), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 1, 0, 1)));
        }

        private void DrawDistance(Entity entity)
        {
            float unitToMeters = 0.01905f; // Коэффициент пересчета
            float distance = Vector3.Distance(localPlayer.position, entity.position) * unitToMeters;
            string distanceText = $"{distance:F1}m";

            Vector2 textSize = ImGui.CalcTextSize(distanceText); // Получаем размер текста
            Vector2 textPosition = new Vector2(entity.position2D.X - (textSize.X / 2), entity.position2D.Y + 5); // Центрируем по X

            drawList.AddText(textPosition, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)), distanceText);
        }

        public void UpdateEntities(IEnumerable<Entity> newEntities)
        {
            entities = new ConcurrentQueue<Entity>(newEntities);
        }

        public void UpdateLocalPlayer(Entity newEntity)
        {
            lock (entityLock)
            {
                localPlayer = newEntity;
            }
        }

        public Entity GetLocalPlayer()
        {
            lock (entityLock)
            {
                return localPlayer;
            }
        }

        void DrawOverlay(Vector2 screenSize)
        {
            ImGui.SetNextWindowSize(screenSize);
            ImGui.SetNextWindowPos(new Vector2(0, 0));
            ImGui.Begin("overlay", ImGuiWindowFlags.NoDecoration
                | ImGuiWindowFlags.NoBackground
                | ImGuiWindowFlags.NoBringToFrontOnFocus
                | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoInputs
                | ImGuiWindowFlags.NoCollapse
                | ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoScrollWithMouse);
        }

        private static Vector2 GetScreenSize()
        {
            int width = GetSystemMetrics(0);
            int height = GetSystemMetrics(1);
            return new Vector2(width, height);
        }

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);
    }
}
