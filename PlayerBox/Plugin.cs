using System;
using System.Numerics;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using PlayerBox.Windows;

namespace PlayerBox;

enum Role
{
    Tank,
    Healer,
    MDps,
    RDps,
}
public sealed class Plugin : IDalamudPlugin
{
    public string Name => "PlayerBox";
    private const string CommandName = "/playerbox";

    private DalamudPluginInterface PluginInterface { get; init; }
    private ICommandManager CommandManager { get; init; }
    private IClientState ClientState { get; init; }
    private IPartyList PartyList { get; init; }
    private IFramework Framework { get; init; }
    private IGameGui GameGui { get; init; }
    private ICondition Condition { get; init; }
    private IPluginLog PluginLog { get; init; }
    public Configuration Configuration { get; init; }
    
    public MainWindow MainWindow { get; init; }
    public ConfigWindow ConfigWindow { get; init; }

    public Plugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] ICommandManager commandManager,
        [RequiredVersion("1.0")] IClientState clientState,
        [RequiredVersion("1.0")] IPartyList partyList,
        [RequiredVersion("1.0")] IFramework framework,
        [RequiredVersion("1.0")] IGameGui gameGui,
        [RequiredVersion("1.0")] ICondition condition,
        [RequiredVersion("1.0")] IPluginLog pluginLog)
    {
        PluginInterface = pluginInterface;
        CommandManager = commandManager;
        ClientState = clientState;
        PartyList = partyList;
        Framework = framework;
        GameGui = gameGui;
        Condition = condition;
        PluginLog = pluginLog;

        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);

        PluginInterface.UiBuilder.Draw += DrawUI;
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= DrawUI;
        CommandManager.RemoveHandler(CommandName);
    }

    private void DrawCross(PartyMember partyMember, float scaleLineBy)
    {
        float distanceToMember = 0;
        if (ClientState.LocalPlayer != null && 
            ClientState.LocalPlayer.ObjectId == partyMember.ObjectId)
        {
            return;
        }
        
        if (ClientState.LocalPlayer != null)
            distanceToMember = float.Abs((partyMember.Position - ClientState.LocalPlayer.Position).Length());
        
        var lineLength = (distanceToMember / 2) * scaleLineBy;
        var verticalStart = partyMember.Position.Z + lineLength;
        var verticalStop = partyMember.Position.Z - lineLength;
        var horizontalStart = partyMember.Position.X - lineLength;
        var horizontalStop = partyMember.Position.X + lineLength;
                 
        GameGui.WorldToScreen(new Vector3(partyMember.Position.X, partyMember.Position.Y, verticalStart), out var verticalStartScreen);
        GameGui.WorldToScreen(new Vector3(partyMember.Position.X, partyMember.Position.Y, verticalStop), out var verticalStopScreen);
        GameGui.WorldToScreen(new Vector3(horizontalStart, partyMember.Position.Y, partyMember.Position.Z), out var horizontalStartScreen);
        GameGui.WorldToScreen(new Vector3(horizontalStop, partyMember.Position.Y, partyMember.Position.Z), out var horizontalStopScreen);
     
        var role = GetRole(partyMember);
        var color = ClientState.LocalPlayer != null &&
                    partyMember.ObjectId == ClientState.LocalPlayer.ObjectId ? 0xFFFFFFFF : GetColorBasedOnDistance(distanceToMember, role);
     
        ImGui.GetWindowDrawList()
             .AddLine(horizontalStartScreen, horizontalStopScreen, color, 1.0f);
        ImGui.GetWindowDrawList()
             .AddLine(verticalStartScreen, verticalStopScreen, color, 1.0f);   
    }

    private static Role GetRole(PartyMember partyMember)
    {
        int memberRole = partyMember.ClassJob.GetWithLanguage(Dalamud.ClientLanguage.English)!.Role;
        var role = memberRole switch
        {
            1 => Role.Tank,
            2 => Role.MDps,
            3 => Role.RDps,
            4 => Role.Healer,
            _ => Role.Healer
        };
        return role;
    }

    private void DrawSquare(PartyMember partyMember, float squareLength)
    {
        if (ClientState.LocalPlayer != null &&
            ClientState.LocalPlayer.ObjectId == partyMember.ObjectId)
        {
            return;
        }
        var topLeftCorner = new Vector3(partyMember.Position.X - (squareLength / 2), partyMember.Position.Y,
                                        partyMember.Position.Z + (squareLength / 2));
        var topRightCorner = new Vector3(partyMember.Position.X + (squareLength / 2), partyMember.Position.Y,
                                        partyMember.Position.Z + (squareLength / 2));
        var bottomRightCorner = new Vector3(partyMember.Position.X + (squareLength / 2), partyMember.Position.Y,
                                        partyMember.Position.Z - (squareLength / 2));
        var bottomLeftCorner = new Vector3(partyMember.Position.X - (squareLength / 2), partyMember.Position.Y,
                                        partyMember.Position.Z - (squareLength / 2));

        GameGui.WorldToScreen(topLeftCorner, out var topLeftCornerScreen);
        GameGui.WorldToScreen(topRightCorner, out var topRightCornerScreen);
        GameGui.WorldToScreen(bottomRightCorner, out var bottomRightCornerScreen);
        GameGui.WorldToScreen(bottomLeftCorner, out var bottomLeftCornerScreen);

        var colour = getColourFromRole(GetRole(partyMember));
        ImGui.GetWindowDrawList()
             .AddLine(topLeftCornerScreen, topRightCornerScreen, colour, 1.0f);
        ImGui.GetWindowDrawList()
             .AddLine(topRightCornerScreen, bottomRightCornerScreen, colour, 1.0f);
        ImGui.GetWindowDrawList()
             .AddLine(bottomRightCornerScreen, bottomLeftCornerScreen, colour, 1.0f);
        ImGui.GetWindowDrawList()
             .AddLine(bottomLeftCornerScreen, topLeftCornerScreen, colour, 1.0f);
    }
        
    private void DrawCanvas()
    {
        if (ClientState.LocalPlayer == null) return;
        var screenWidth = ImGui.GetWindowWidth();
        var screenHeight = ImGui.GetWindowHeight();
        foreach (var partyMember in PartyList)
        {
            DrawCross(partyMember, 0.5f);
            DrawSquare(partyMember, 1.0f);
        }
    }



    // Function to calculate color based on distance
    private uint GetColorBasedOnDistance(float distance, Role role) {
        // Full opacity
        const int alpha = 255;         

        var baseColor = getColourFromRole(role);

        // Extract RGB components from the base color
        int red = (int)(baseColor >> 16) & 0xFF;
        int green = (int)(baseColor >> 8) & 0xFF;
        int blue = (int)baseColor & 0xFF;

        // Normalize brightness based on distance
        int minBrightness = 255;  // Minimum brightness to ensure visibility
        int maxBrightness = 255; // Maximum brightness
        int brightness;
        if (distance > 10)
            brightness = maxBrightness;
        else
            brightness = Math.Clamp((int)((distance / 10 * (maxBrightness - minBrightness)) + minBrightness), minBrightness, maxBrightness);

        // Scale RGB components according to calculated brightness
        red = (red * brightness) / maxBrightness;
        green = (green * brightness) / maxBrightness;
        blue = (blue * brightness) / maxBrightness;

        return ((uint)alpha << 24) | ((uint)red << 16) | ((uint)green << 8) | (uint)blue;
    }

    private static uint getColourFromRole(Role role)
    {
        // Define color codes for each role
        const uint colorTank = 0xFFFF3333;   // Blue
        const uint colorMDps = 0xFF0000FF;   // Red
        const uint colorRDps = 0xFF0000FF;   // Red
        const uint colorHealer = 0xFF00FF00; // Green

        // Select color based on role
        uint baseColor = role switch
        {
            Role.Tank => colorTank,
            Role.MDps => colorMDps,
            Role.RDps => colorRDps,
            Role.Healer => colorHealer,
            _ => 0xFFFFFFFF
        };
        return baseColor;
    }


    private void DrawUI()
    {
        if (ClientState.LocalPlayer == null) return; 
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
        ImGuiHelpers.ForceNextWindowMainViewport();
        ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(0, 0));
        ImGui.Begin("Canvas",
                    ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoTitleBar |
                    ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground);
        ImGui.SetWindowSize(ImGui.GetIO().DisplaySize);
        DrawCanvas();
        ImGui.End();
        ImGui.PopStyleVar();
    }
}
