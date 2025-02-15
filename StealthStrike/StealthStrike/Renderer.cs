using System;
using System.Collections.Concurrent;
using System.Numerics;
using ClickableTransparentOverlay;
using TextCopy;
using ImGuiNET;
using System.Diagnostics;

namespace BasicESP
{
    public class Renderer : Overlay
    {
        public Vector2 screenSize = new Vector2(2560, 1080);

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

            // Кнопка для копирования ссылки на Discord-сервер
            if (ImGui.Button("Copy My Discord"))
            {
                ClipboardService.SetText("https://discord.gg/your-server-link");
            }

            if (ImGui.Button("Copy My Telegram"))
            {
                ClipboardService.SetText("https://t.me/your-telegram-link");
            }

            // Кнопка для открытия GitHub
            if (ImGui.Button("Open My GitHub"))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/ghfakegh1337/StealthStrike", // Укажите вашу ссылку
                    UseShellExecute = true
                });
            }

            DrawOverlay(screenSize);
            drawList = ImGui.GetWindowDrawList();

            if (enableESP)
            {
                foreach (var entity in entities)
                {
                    if (EntityOnScreen(entity))
                    {
                        DrawBox(entity);
                        DrawLine(entity);
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
    }
}
