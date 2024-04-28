using System.Numerics;
using Dalamud.Game.Command;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ImGuiNET;
using PlayerBox.Windows;

namespace PlayerBox;

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

    private void DrawCanvas()
    {
        var screenWidth = ImGui.GetWindowWidth();
        var screenHeight = ImGui.GetWindowHeight();
        foreach (var partyMember in PartyList)
        {
            var verticalStart = partyMember.Position.Y + 10;
            var verticalStop = partyMember.Position.Y - 10;
            var horizontalStart = partyMember.Position.X - 10;
            var horizontalStop = partyMember.Position.X + 10;
            var z = partyMember.Position.Z;

            GameGui.WorldToScreen(new Vector3(partyMember.Position.X, verticalStart, z), out var verticalStartScreen);
            GameGui.WorldToScreen(new Vector3(partyMember.Position.X, verticalStop, z), out var verticalStopScreen);
            GameGui.WorldToScreen(new Vector3(horizontalStart, partyMember.Position.Y, z), out var horizontalStartScreen);
            GameGui.WorldToScreen(new Vector3(horizontalStop, partyMember.Position.Y, z), out var horizontalStopScreen);

            ImGui.GetWindowDrawList()
                 .AddLine(horizontalStartScreen, horizontalStopScreen, 0xFFFFFFFF, 1.0f);
            ImGui.GetWindowDrawList()
                 .AddLine(verticalStartScreen, verticalStopScreen, 0xFFFFFFFF, 1.0f);
        }
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
