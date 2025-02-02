using Avalonia.Input;
using System.Globalization;
using StudioCommunication;
using TAS.Avalonia.Communication;

namespace TAS.Avalonia.Services;

public class CelesteService {
    private Dictionary<HotkeyID, List<Keys>> _bindings;
    private StudioInfo _state;

    public StudioCommunicationServer Server { get; }

    public CelesteService() {
        Server = new StudioCommunicationServer();
        Server.BindingsUpdated += bindings => _bindings = bindings;
        Server.StateUpdated += state => _state = state;
        Server.Run();
    }

    public void WriteWait() => Server.WriteWait();
    public void SendPath(string path) => Server.SendPath(path);

    public bool SendKeyEvent(IEnumerable<Key> keys, bool released) {
        var winFormsKeys = keys.Select(key => key.ToWinForms()).ToArray();
        bool pressedAny = false;

        foreach (HotkeyID hotkeyIDs in _bindings.Keys) {
            List<Keys> bindingKeys = _bindings[hotkeyIDs];

            bool pressed = bindingKeys.Count > 0 && bindingKeys.All(key => winFormsKeys.Contains(key));
            if (pressed && bindingKeys.Count == 1) {
                // Don't trigger a hotkey without a modifier if a modifier is pressed
                if (!bindingKeys.Contains(Keys.LShiftKey) && !bindingKeys.Contains(Keys.RShiftKey) && winFormsKeys.Any(key => key is Keys.LShiftKey or Keys.RShiftKey)) {
                    pressed = false;
                }
                if (!bindingKeys.Contains(Keys.LControlKey) && !bindingKeys.Contains(Keys.RControlKey) && winFormsKeys.Any(key => key is Keys.LControlKey or Keys.RControlKey)) {
                    pressed = false;
                }
                if (!bindingKeys.Contains(Keys.LMenu) && !bindingKeys.Contains(Keys.RMenu) && winFormsKeys.Any(key => key is Keys.LMenu or Keys.RMenu)) {
                    pressed = false;
                }
            }

            if (pressed) {
                pressedAny = true;
                // if (hotkeyIDs == HotkeyID.FastForward) {
                //     fastForwarding = true;
                // } else if (hotkeyIDs == HotkeyID.SlowForward) {
                //     slowForwarding = true;
                // }

                Server.SendHotkeyPressed(hotkeyIDs, released);
            }
        }

        return pressedAny;
    }

    public void Play() {
    }

    public bool Connected => StudioCommunicationBase.Initialized;

    public int CurrentLine => Connected ? _state.CurrentLine : -1;
    public string CurrentLineSuffix => Connected ? _state.CurrentLineSuffix : string.Empty;
    public int CurrentFrameInTas => Connected ? _state.CurrentFrameInTas : -1;
    public int TotalFrames => Connected ? _state.TotalFrames : -1;
    public int SaveStateLine => Connected ? _state.SaveStateLine : -1;
    public States TasStates => Connected ? (States) _state.tasStates : States.None;
    public string GameInfo => Connected ? _state.GameInfo : string.Empty;
    public string LevelName => Connected ? _state.LevelName : string.Empty;
    public string ChapterTime => Connected ? _state.ChapterTime : string.Empty;

    public void ToggleHitboxes() => Server.ToggleGameSetting("ShowHitboxes", null);
    public void ToggleTriggerHitboxes() => Server.ToggleGameSetting("ShowTriggerHitboxes", null);
    public void ToggleUnloadedRoomsHitboxes() => Server.ToggleGameSetting("ShowUnloadedRoomsHitboxes", null);
    public void ToggleCameraHitboxes() => Server.ToggleGameSetting("ShowCameraHitboxes", null);
    public void ToggleSimplifiedHitboxes() => Server.ToggleGameSetting("SimplifiedHitboxes", null);
    public void ToggleActualCollideHitboxes() => Server.ToggleGameSetting("ShowActualCollideHitboxes", null);
    public void ToggleSimplifiedGraphics() => Server.ToggleGameSetting("SimplifiedGraphics", null);
    public void ToggleGameplay() => Server.ToggleGameSetting("ShowGameplay", null);
    public void ToggleCenterCamera() => Server.ToggleGameSetting("CenterCamera", null);
    public void ToggleInfoHud() => Server.ToggleGameSetting("InfoHud", null);
    public void ToggleInfoTasInput() => Server.ToggleGameSetting("InfoTasInput", null);
    public void ToggleInfoGame() => Server.ToggleGameSetting("InfoGame", null);
    public void ToggleInfoWatchEntity() => Server.ToggleGameSetting("InfoWatchEntity", null);
    public void ToggleInfoCustom() => Server.ToggleGameSetting("InfoCustom", null);
    public void ToggleInfoSubpixelIndicator() => Server.ToggleGameSetting("InfoSubpixelIndicator", null);
    public void ToggleSpeedUnit() => Server.ToggleGameSetting("SpeedUnit", null);

    private const int DefaultDecimals = 2;
    private const int DefaultFastForwardSpeed = 10;
    private const float DefaultSlowForwardSpeed = 0.1f;

    private int GetDecimals(string settingName) {
        string decimals = DefaultDecimals.ToString();
        if (Server.GetDataFromGame(GameDataType.SettingValue, settingName) is { } settingValue) {
            decimals = settingValue;
        }

        bool success = int.TryParse(decimals, out int result);
        return success ? result : DefaultDecimals;
    }

    public int GetPositionDecimals() => GetDecimals("PositionDecimals");
    public void SetPositionDecimals(int value) => Server.ToggleGameSetting("PositionDecimals", value);

    public int GetSpeedDecimals() => GetDecimals("SpeedDecimals");
    public void SetSpeedDecimals(int value) => Server.ToggleGameSetting("SpeedDecimals", value);

    public int GetVelocityDecimals() => GetDecimals("VelocityDecimals");
    public void SetVelocityDecimals(int value) => Server.ToggleGameSetting("VelocityDecimals", value);

    public int GetCustomInfoDecimals() => GetDecimals("CustomInfoDecimals");
    public void SetCustomInfoDecimals(int value) => Server.ToggleGameSetting("CustomInfoDecimals", value);

    public int GetSubpixelIndicatorDecimals() => GetDecimals("SubpixelIndicatorDecimals");
    public void SetSubpixelIndicatorDecimals(int value) => Server.ToggleGameSetting("SubpixelIndicatorDecimals", value);

    public int GetFastForwardSpeed() {
        string speed = DefaultFastForwardSpeed.ToString();
        if (Server.GetDataFromGame(GameDataType.SettingValue, "FastForwardSpeed") is { } settingValue) {
            speed = settingValue;
        }

        bool success = int.TryParse(speed, out int result);
        return success ? result : DefaultFastForwardSpeed;
    }
    public void SetFastForwardSpeed(int value) => Server.ToggleGameSetting("FastForwardSpeed", value);

    public float GetSlowForwardSpeed() {
        string speed = DefaultSlowForwardSpeed.ToString(CultureInfo.InvariantCulture);
        if (Server.GetDataFromGame(GameDataType.SettingValue, "SlowForwardSpeed") is { } settingValue) {
            speed = settingValue;
        }

        bool success = float.TryParse(speed, NumberStyles.None, CultureInfo.InvariantCulture, out float result);
        return success ? result : DefaultSlowForwardSpeed;
    }
    public void SetSlowForwardSpeed(float value) => Server.ToggleGameSetting("SlowForwardSpeed", value);
}
