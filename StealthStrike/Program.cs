﻿using System;
using System.Numerics;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using BasicESP;
using Swed64;

public class Program
{
    private static readonly string validKey = "KP69Z-O2V6U-YU1SN-XLLRU-KAGPZ";
    static void Main()
    {
        ValidateInputKey(validKey);

        Swed swed = new Swed("cs2");

        IntPtr client = swed.GetModuleBase("client.dll");

        // offsets.cs
        int dwEntityList = 0x1A1F730;
        int dwViewMatrix = 0x1A89130;
        int dwLocalPlayerPawn = 0x1874050;

        // client_dll.cs
        int m_iIDEntIndex = 0x1458;
        int m_vOldOrigin = 0x1324;
        int m_iTeamNum = 0x3E3;
        int m_lifeState = 0x348;
        int m_hPlayerPawn = 0x814;
        int m_vecViewOffset = 0xCB0;
        int m_iHealth = 0x344; // Добавляем оффсет для здоровья

        Renderer renderer = new Renderer();
        Thread renderThread = new Thread(new ThreadStart(renderer.Start().Wait));
        renderThread.Start();

        Vector2 screenSize = renderer.screenSize;

        List<Entity> entities = new List<Entity>();
        Entity localPlayer = new Entity();

        // Поток триггера
        Thread triggerThread = new Thread(() =>
        {
            while (true)
            {
                if (renderer.EnableTrigger)
                {
                    IntPtr entityList = swed.ReadPointer(client, dwEntityList);
                    IntPtr localPlayerPawn = swed.ReadPointer(client, dwLocalPlayerPawn);

                    int team = swed.ReadInt(localPlayerPawn, m_iTeamNum);
                    int entIndex = swed.ReadInt(localPlayerPawn, m_iIDEntIndex);

                    if (entIndex != -1)
                    {
                        IntPtr entityListEntry = swed.ReadPointer(entityList, 0x8 * ((entIndex & 0x7FFF) >> 9) + 0x10);
                        IntPtr currentPawn = swed.ReadPointer(entityListEntry, 0x78 * (entIndex & 0x1FF));
                        int entityTeam = swed.ReadInt(currentPawn, m_iTeamNum);
                        int targetHealth = swed.ReadInt(currentPawn, m_iHealth);

                        int HOTKEY = renderer.SelectedKey;
                        int MOUSEEVENTF_LEFTDOWN = 0x02;
                        int MOUSEEVENTF_LEFTUP = 0x04;

                        if (team != entityTeam && targetHealth > 0 && GetAsyncKeyState(HOTKEY) < 0)
                        {
                            Thread.Sleep(renderer.triggerDeleyBeforeShot);
                            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                            Thread.Sleep(renderer.triggerDeleyAfterShot);
                        }
                    }
                }
            }
        });
        triggerThread.IsBackground = true;
        triggerThread.Start();

        while (true)
        {
            entities.Clear();

            IntPtr entityList = swed.ReadPointer(client, dwEntityList);

            IntPtr listEntry = swed.ReadPointer(entityList, 0x10);

            IntPtr localPlayerPawn = swed.ReadPointer(client, dwLocalPlayerPawn);

            localPlayer.team = swed.ReadInt(localPlayerPawn, m_iTeamNum);
            localPlayer.position = swed.ReadVec(localPlayerPawn, m_vOldOrigin);


            for (int i = 0; i < 64; i++)
            {
                IntPtr currentController = swed.ReadPointer(listEntry, i * 0x78);

                if (currentController == IntPtr.Zero) continue;

                int pawnHandle = swed.ReadInt(currentController, m_hPlayerPawn);
                if (pawnHandle == 0) continue;

                IntPtr listEntry2 = swed.ReadPointer(entityList, 0x8 * ((pawnHandle & 0x7FFF) >> 9) + 0x10);
                if (listEntry2 == IntPtr.Zero) continue;

                IntPtr currentPawn = swed.ReadPointer(listEntry2, 0x78 * (pawnHandle & 0x1FF));
                if (currentPawn == IntPtr.Zero) continue;

                int lifeState = swed.ReadInt(currentPawn, m_lifeState);
                if (lifeState != 256) continue;

                float[] viewMatrix = swed.ReadMatrix(client + dwViewMatrix);

                Entity entity = new Entity();

                entity.team = swed.ReadInt(currentPawn, m_iTeamNum);
                entity.position = swed.ReadVec(currentPawn, m_vOldOrigin);
                entity.viewOffset = swed.ReadVec(currentPawn, m_vecViewOffset);
                entity.position2D = Calculate.WorldToScreen(viewMatrix, entity.position, screenSize);
                entity.viewPosition2D = Calculate.WorldToScreen(viewMatrix, Vector3.Add(entity.position, entity.viewOffset), screenSize);
                entity.health = swed.ReadInt(currentPawn, m_iHealth); // Читаем здоровье

                entities.Add(entity);
            }

            renderer.UpdateLocalPlayer(localPlayer);
            renderer.UpdateEntities(entities);
        }
    }

    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    static extern void mouse_event(long dwFlags, long dx, long dy, long cButtons, long dwExtraInfo);

    static void ValidateInputKey(string validKey)
    {
        // Запрашиваем ключ
        string? userInputKey;

        // Проверяем введенный ключ
        do
        {
            userInputKey = Console.ReadLine();
        } while (userInputKey != validKey);

        Console.ForegroundColor = ConsoleColor.DarkRed;
    }
}
