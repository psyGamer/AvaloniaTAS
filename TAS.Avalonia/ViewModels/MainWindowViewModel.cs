using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using AvaloniaEdit;
using ReactiveUI;
using TAS.Avalonia.Controls;
using TAS.Avalonia.Editing;
using TAS.Avalonia.Models;
using TAS.Avalonia.Services;
using TAS.Avalonia.Views;

namespace TAS.Avalonia.ViewModels;

public class MainWindowViewModel : ViewModelBase {
    // File
    public ReactiveCommand<Unit, Unit> NewFileCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenFileCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveFileCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveFileAsCommand { get; }
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }

    // Settings
    public ReactiveCommand<Unit, Unit> ToggleSendInputsCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleGameInfoCommand { get; }

    // Toggles
    public ReactiveCommand<Unit, Unit> ToggleHitboxesCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleTriggerHitboxesCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleUnloadedRoomsHitboxesCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleCameraHitboxesCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleSimplifiedHitboxesCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleActualCollideHitboxesCommand { get; }

    public ReactiveCommand<Unit, Unit> ToggleSimplifiedGraphicsCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleGameplayCommand { get; }

    public ReactiveCommand<Unit, Unit> ToggleCenterCameraCommand { get; }

    public ReactiveCommand<Unit, Unit> ToggleInfoHudCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleInfoTasInputCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleInfoGameCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleInfoWatchEntityCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleInfoCustomCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleInfoSubpixelIndicatorCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleUnitOfSpeedCommand { get; }

    public ReactiveCommand<Unit, Unit> SetPositionDecimalsCommand { get; }
    public ReactiveCommand<Unit, Unit> SetSpeedDecimalsCommand { get; }
    public ReactiveCommand<Unit, Unit> SetVelocityDecimalsCommand { get; }
    public ReactiveCommand<Unit, Unit> SetCustomInfoDecimalsCommand { get; }
    public ReactiveCommand<Unit, Unit> SetSubpixelIndicatorDecimalsCommand { get; }

    public ReactiveCommand<Unit, Unit> SetFastForwardSpeedCommand { get; }
    public ReactiveCommand<Unit, Unit> SetSlowForwardSpeedCommand { get; }

    private readonly ObservableAsPropertyHelper<string> _windowTitle;
    public string WindowTitle => _windowTitle.Value;

    private string _statusText = "Searching...";
    public string StatusText {
        get => _statusText;
        set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }

    private TASDocument _document;
    public TASDocument Document {
        get => _document;
        set => this.RaiseAndSetIfChanged(ref _document, value);
    }

    private TextViewPosition _caretPosition;
    public TextViewPosition CaretPosition {
        get => _caretPosition;
        set => this.RaiseAndSetIfChanged(ref _caretPosition, value);
    }

    public bool GameInfoVisible {
        get => _settingsService.GameInfoVisible;
        set {
            _settingsService.GameInfoVisible = value;
            this.RaisePropertyChanged();
        }
    }

    public bool MenuVisible => true; //!RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    private readonly EditorControl _editor;

    private readonly CelesteService _celesteService;
    private readonly DialogService _dialogService;
    private readonly SettingsService _settingsService;

    private MenuModel[] MainMenu { get; }
    private MenuModel[] EditorContextMenu { get; }

    private FilePickerFileType _tasFileType = new FilePickerFileType("CelesteTAS") {
        Patterns = new[] { "*.tas" },
        MimeTypes = new[] { "text/plain" }, // ? Maybe add a CelesteTAS MIME-Type
        AppleUniformTypeIdentifiers = new[] { "public.item" }, // TODO: replace this with custom
    };

    public MainWindowViewModel(MainWindow window) {
        _editor = window.FindControl<EditorControl>("editor");

        _celesteService = (Application.Current as App)!.CelesteService;
        _dialogService = (Application.Current as App)!.DialogService;
        _settingsService = (Application.Current as App)!.SettingsService;

        _windowTitle = this.WhenAnyValue(x => x.Document.FileName, x => x.Document.Dirty, (path, dirty) => Tuple.Create(path, dirty))
                           .Select(t => $"TAS Studio{(t.Item1 != null ? $" - {(t.Item2 ? "*" : "")}{t.Item1}" : "")}")
                           .ToProperty(this, nameof(WindowTitle));
        _celesteService.Server.StateUpdated += state => {
            StatusText = state.GameInfo;
        };

        // File
        NewFileCommand = ReactiveCommand.Create(NewFile);
        OpenFileCommand = ReactiveCommand.Create(OpenFile);
        SaveFileCommand = ReactiveCommand.Create(SaveFile);
        SaveFileAsCommand = ReactiveCommand.Create(SaveFileAs);
        ExitCommand = ReactiveCommand.Create(Exit);

        // Settings
        ToggleSendInputsCommand = ReactiveCommand.Create(ToggleSendInputs);
        ToggleGameInfoCommand = ReactiveCommand.Create(ToggleGameInfo);

        // Toggles
        ToggleHitboxesCommand = ReactiveCommand.Create(ToggleHitboxes);
        ToggleTriggerHitboxesCommand = ReactiveCommand.Create(ToggleTriggerHitboxes);
        ToggleUnloadedRoomsHitboxesCommand = ReactiveCommand.Create(ToggleUnloadedRoomsHitboxes);
        ToggleCameraHitboxesCommand = ReactiveCommand.Create(ToggleCameraHitboxes);
        ToggleSimplifiedHitboxesCommand = ReactiveCommand.Create(ToggleSimplifiedHitboxes);
        ToggleActualCollideHitboxesCommand = ReactiveCommand.Create(ToggleActualCollideHitboxes);

        ToggleSimplifiedGraphicsCommand = ReactiveCommand.Create(ToggleSimplifiedGraphics);
        ToggleGameplayCommand = ReactiveCommand.Create(ToggleGameplay);

        ToggleCenterCameraCommand = ReactiveCommand.Create(ToggleCenterCamera);

        ToggleInfoHudCommand = ReactiveCommand.Create(ToggleInfoHud);
        ToggleInfoTasInputCommand = ReactiveCommand.Create(ToggleInfoTasInput);
        ToggleInfoGameCommand = ReactiveCommand.Create(ToggleInfoGame);
        ToggleInfoWatchEntityCommand = ReactiveCommand.Create(ToggleInfoWatchEntity);
        ToggleInfoCustomCommand = ReactiveCommand.Create(ToggleInfoCustom);
        ToggleInfoSubpixelIndicatorCommand = ReactiveCommand.Create(ToggleInfoSubpixelIndicator);
        ToggleUnitOfSpeedCommand = ReactiveCommand.Create(ToggleUnitOfSpeed);

        SetPositionDecimalsCommand = ReactiveCommand.CreateFromTask(SetPositionDecimals);
        SetSpeedDecimalsCommand = ReactiveCommand.CreateFromTask(SetSpeedDecimals);
        SetVelocityDecimalsCommand = ReactiveCommand.CreateFromTask(SetVelocityDecimals);
        SetCustomInfoDecimalsCommand = ReactiveCommand.CreateFromTask(SetCustomInfoDecimals);
        SetSubpixelIndicatorDecimalsCommand = ReactiveCommand.CreateFromTask(SetSubpixelIndicatorDecimals);

        SetFastForwardSpeedCommand = ReactiveCommand.CreateFromTask(SetFastForwardSpeed);
        SetSlowForwardSpeedCommand = ReactiveCommand.CreateFromTask(SetSlowForwardSpeed);

        var lastOpenFilePath = _settingsService.LastOpenFilePath;

        _celesteService.WriteWait();
        if (Path.Exists(lastOpenFilePath)) {
            Document = TASDocument.Load(lastOpenFilePath);
        }
        Document ??= TASDocument.CreateBlank();
        if (Document.FilePath != null) _celesteService.SendPath(Document.FilePath);

        MainMenu = CreateMenu(MenuVisible);
        EditorContextMenu = CreateContextMenu();
    }

    private MenuModel[] CreateMenu(bool includeExit) {
        var commandModifier = KeyModifiers.Control;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            commandModifier = KeyModifiers.Meta;
        }

        return new[] {
            new MenuModel("_File", isEnabled: true) {
                new MenuModel("New File", NewFileCommand, gesture: new KeyGesture(Key.N, commandModifier)),
                MenuModel.Separator,
                new MenuModel("Open File...", OpenFileCommand, gesture: new KeyGesture(Key.O, commandModifier)),
                new MenuModel("Open Previous File"),
                new MenuModel("Open Recent") {
                    new MenuModel("Celeste.tas"),
                },
                new MenuModel("Open Backup") {
                    new MenuModel("Celeste.tas"),
                },
                MenuModel.Separator,
                new MenuModel("Save", SaveFileCommand, gesture: new KeyGesture(Key.S, commandModifier)),
                new MenuModel("Save As...", SaveFileAsCommand, gesture: new KeyGesture(Key.S, commandModifier | KeyModifiers.Shift)),
                new MenuModel("Convert to LibTAS Inputs..."),
                new MenuModel(string.Empty, isVisible: includeExit),
                new MenuModel("Exit", ExitCommand, isVisible: includeExit),
            },
            new MenuModel("Settings") {
                new MenuModel("Send Inputs to Celeste", ToggleSendInputsCommand, gesture: new KeyGesture(Key.D, commandModifier)),
                new MenuModel("Auto Remove Mutually Exclusive Actions"),
                new MenuModel("Show Game Info", ToggleGameInfoCommand),
                new MenuModel("Automatic Backup") {
                    new MenuModel("Enabled"),
                    new MenuModel("Backup Rate (minutes): 1"),
                    new MenuModel("Backup File Count: 100"),
                },
                new MenuModel("Font..."),
                new MenuModel("Themes") {
                    new MenuModel("Light"),
                    new MenuModel("Dark"),
                    new MenuModel("Custom"),
                },
            },
            new MenuModel("Toggles") {
                new MenuModel("Hitboxes", command: ToggleHitboxesCommand),
                new MenuModel("Trigger Hitboxes", command: ToggleTriggerHitboxesCommand),
                new MenuModel("Unloaded Rooms Hitboxes", command: ToggleUnloadedRoomsHitboxesCommand),
                new MenuModel("Camera Hitboxes", command: ToggleCameraHitboxesCommand),
                new MenuModel("Simplified Hitboxes", command: ToggleSimplifiedHitboxesCommand),
                new MenuModel("Actual Collide Hitboxes", command: ToggleActualCollideHitboxesCommand),
                MenuModel.Separator,
                new MenuModel("Simplified Graphics", command: ToggleSimplifiedGraphicsCommand),
                new MenuModel("Gameplay", command: ToggleGameplayCommand),
                MenuModel.Separator,
                new MenuModel("Center Camera", command: ToggleCenterCameraCommand),
                MenuModel.Separator,
                new MenuModel("Info HUD", command: ToggleInfoHudCommand),
                new MenuModel("TAS Input Info", command: ToggleInfoTasInputCommand),
                new MenuModel("Game Info", command: ToggleInfoGameCommand),
                new MenuModel("Watch Entity Info", command: ToggleInfoWatchEntityCommand),
                new MenuModel("Custom Info", command: ToggleInfoCustomCommand),
                new MenuModel("Subpixel Indicator", command: ToggleInfoSubpixelIndicatorCommand),
                new MenuModel("Unit of Speed", command: ToggleUnitOfSpeedCommand),
                MenuModel.Separator,
                new MenuModel("Position Decimals", command: SetPositionDecimalsCommand),
                new MenuModel("Speed Decimals", command: SetSpeedDecimalsCommand),
                new MenuModel("Velocity Decimals", command: SetVelocityDecimalsCommand),
                new MenuModel("Custom Info Decimals", command: SetCustomInfoDecimalsCommand),
                new MenuModel("Subpixel Indicator Decimals", command: SetSubpixelIndicatorDecimalsCommand),
                MenuModel.Separator,
                new MenuModel("Fast Forward Speed", command: SetFastForwardSpeedCommand),
                new MenuModel("Slow Forward Speed", command: SetSlowForwardSpeedCommand),
            },
        };
    }

    private MenuModel[] CreateContextMenu() => new[] {
        new MenuModel("Cut", routedCommand: ApplicationCommands.Cut, textArea: _editor.editor.TextArea),
        new MenuModel("Copy", routedCommand: ApplicationCommands.Copy, textArea: _editor.editor.TextArea),
        new MenuModel("Paste", routedCommand: ApplicationCommands.Paste, textArea: _editor.editor.TextArea),
        MenuModel.Separator,
        new MenuModel("Undo", routedCommand: ApplicationCommands.Undo, textArea: _editor.editor.TextArea),
        new MenuModel("Redo", routedCommand: TASInputHandler.Redo, textArea: _editor.editor.TextArea),
        MenuModel.Separator,
        new MenuModel("Select All", routedCommand: ApplicationCommands.SelectAll, textArea: _editor.editor.TextArea),
        new MenuModel("Select Block", routedCommand: TASCaretNavigationCommandHandler.SelectBlock, textArea: _editor.editor.TextArea),
        MenuModel.Separator,
        new MenuModel("Insert/Remove Breakpoint"),
        new MenuModel("Insert/Remove Savestate Breakpoint"),
        new MenuModel("Remove All Uncommented Breakpoints"),
        new MenuModel("Remove All Breakpoints"),
        new MenuModel("Comment/Uncomment All Breakpoints"),
        new MenuModel("Comment/Uncomment Inputs", routedCommand: TASEditingCommandHandler.ToggleCommentInputs, textArea: _editor.editor.TextArea),
        new MenuModel("Comment/Uncomment Text", routedCommand: TASEditingCommandHandler.ToggleCommentText, textArea: _editor.editor.TextArea),
        MenuModel.Separator,
        new MenuModel("Insert Room Name", routedCommand: TASEditingCommandHandler.InsertRoomName, textArea: _editor.editor.TextArea),
        new MenuModel("Insert Current In-Game Time", routedCommand: TASEditingCommandHandler.InsertTime, textArea: _editor.editor.TextArea),
        new MenuModel("Insert Mod Info"),
        new MenuModel("Insert Console Load Command"),
        new MenuModel("Insert Simple Console Load Command"),
        new MenuModel("Insert Other Command") {
            new MenuModel("EnforceLegal", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "EnforceLegal", textArea: _editor.editor.TextArea),
            new MenuModel("Unsafe", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "Unsafe", textArea: _editor.editor.TextArea),
            new MenuModel("Safe", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "Safe", textArea: _editor.editor.TextArea),
            MenuModel.Separator,
            new MenuModel("Read", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "Read, File Name, Starting Line, (Ending Line)", textArea: _editor.editor.TextArea),
            new MenuModel("Play", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "Play, Starting Line", textArea: _editor.editor.TextArea),
            MenuModel.Separator,
            new MenuModel("Repeat", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "Repeat 2\n\nEndRepeat", textArea: _editor.editor.TextArea),
            new MenuModel("EndRepeat", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "EndRepeat", textArea: _editor.editor.TextArea),
            MenuModel.Separator,
            new MenuModel("Set", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "Set, (Mod).Setting, Value", textArea: _editor.editor.TextArea),
            new MenuModel("Invoke", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "Invoke, Entity.Method, Parameter", textArea: _editor.editor.TextArea),
            new MenuModel("EvalLua", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "EvalLua, Code", textArea: _editor.editor.TextArea),
            MenuModel.Separator,
            new MenuModel("Press", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "Press, Key1, Key2...", textArea: _editor.editor.TextArea),
            MenuModel.Separator,
            new MenuModel("AnalogMode", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "AnalogMode, Ignore/Circle/Square/Precise", textArea: _editor.editor.TextArea),
            MenuModel.Separator,
            new MenuModel("StunPause", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "StunPause\n\nEndStunPause", textArea: _editor.editor.TextArea),
            new MenuModel("EndStunPause", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "EndStunPause", textArea: _editor.editor.TextArea),
            new MenuModel("StunPauseMode", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "StunPauseMode, Simulate/Input", textArea: _editor.editor.TextArea),
            MenuModel.Separator,
            new MenuModel("AutoInput", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "AutoInput, 2\n   1,S,N\n  10,O\nStartAutoInput\n\nEndAutoInput", textArea: _editor.editor.TextArea),
            new MenuModel("StartAutoInput", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "StartAutoInput", textArea: _editor.editor.TextArea),
            new MenuModel("EndAutoInput", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "EndAutoInput", textArea: _editor.editor.TextArea),
            new MenuModel("SkipInput", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "SkipInput", textArea: _editor.editor.TextArea),
            MenuModel.Separator,
            new MenuModel("SaveAndQuitReenter", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "SaveAndQuitReenter", textArea: _editor.editor.TextArea),
            MenuModel.Separator,
            new MenuModel("ExportGameInfo", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "ExportGameInfo dump.txt", textArea: _editor.editor.TextArea),
            new MenuModel("EndExportGameInfo", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "EndExportGameInfo", textArea: _editor.editor.TextArea),
            MenuModel.Separator,
            new MenuModel("ExportRoomInfo", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "ExportRoomInfo dump_room_info.txt", textArea: _editor.editor.TextArea),
            new MenuModel("EndExportRoomInfo", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "EndExportRoomInfo", textArea: _editor.editor.TextArea),
            MenuModel.Separator,
            new MenuModel("StartRecording", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "StartRecording", textArea: _editor.editor.TextArea),
            new MenuModel("StopRecording", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "StopRecording", textArea: _editor.editor.TextArea),
            MenuModel.Separator,
            new MenuModel("Add", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "Add, (input line)", textArea: _editor.editor.TextArea),
            new MenuModel("Skip", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "Skip", textArea: _editor.editor.TextArea),
            new MenuModel("Marker", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "Marker", textArea: _editor.editor.TextArea),
            new MenuModel("ExportLibTAS", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "ExportLibTAS Celeste.ltm", textArea: _editor.editor.TextArea),
            new MenuModel("EndExportLibTAS", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "EndExportLibTAS", textArea: _editor.editor.TextArea),
            MenuModel.Separator,
            new MenuModel("CompleteInfo", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "CompleteInfo A 1", textArea: _editor.editor.TextArea),
            new MenuModel("RecordCount", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "RecordCount: 1", textArea: _editor.editor.TextArea),
            new MenuModel("FileTime", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "FileTime:", textArea: _editor.editor.TextArea),
            new MenuModel("ChapterTime", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "ChapterTime:", textArea: _editor.editor.TextArea),
            new MenuModel("MidwayFileTime", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "MidwayFileTime:", textArea: _editor.editor.TextArea),
            new MenuModel("MidwayChapterTime", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "MidwayChapterTime:", textArea: _editor.editor.TextArea),
            MenuModel.Separator,
            new MenuModel("Exit Game", routedCommand: TASEditingCommandHandler.InsertCommand, commandParameter: "EnforceLegal", textArea: _editor.editor.TextArea),
        },
        MenuModel.Separator,
        new MenuModel("Swap Selected C and X"),
        new MenuModel("Swap Selected J and K"),
        new MenuModel("Combine Consecutive Same Inputs"),
        new MenuModel("Force Combine Inputs Frames"),
        new MenuModel("Convert Dash to Demo Dash"),
        MenuModel.Separator,
        new MenuModel("Open Read File / Go to Play Line"),
    };

    private void ToggleSendInputs() => _settingsService.SendInputs = !_settingsService.SendInputs;
    private void ToggleGameInfo() => GameInfoVisible = !GameInfoVisible;

    private void ToggleHitboxes() => _celesteService.ToggleHitboxes();
    private void ToggleTriggerHitboxes() => _celesteService.ToggleTriggerHitboxes();
    private void ToggleUnloadedRoomsHitboxes() => _celesteService.ToggleUnloadedRoomsHitboxes();
    private void ToggleCameraHitboxes() => _celesteService.ToggleCameraHitboxes();
    private void ToggleSimplifiedHitboxes() => _celesteService.ToggleSimplifiedHitboxes();
    private void ToggleActualCollideHitboxes() => _celesteService.ToggleActualCollideHitboxes();
    private void ToggleSimplifiedGraphics() => _celesteService.ToggleSimplifiedGraphics();
    private void ToggleGameplay() => _celesteService.ToggleGameplay();
    private void ToggleCenterCamera() => _celesteService.ToggleCenterCamera();
    private void ToggleInfoHud() => _celesteService.ToggleInfoHud();
    private void ToggleInfoTasInput() => _celesteService.ToggleInfoTasInput();
    private void ToggleInfoGame() => _celesteService.ToggleInfoGame();
    private void ToggleInfoWatchEntity() => _celesteService.ToggleInfoWatchEntity();
    private void ToggleInfoCustom() => _celesteService.ToggleInfoCustom();
    private void ToggleInfoSubpixelIndicator() => _celesteService.ToggleInfoSubpixelIndicator();
    private void ToggleUnitOfSpeed() => _celesteService.ToggleSpeedUnit();

    private const int MinDecimals = 2;
    private const int MaxDecimals = 12;
    private const int MinFastForwardSpeed = 2;
    private const int MaxFastForwardSpeed = 30;
    private const float MinSlowForwardSpeed = 0.1f;
    private const float MaxSlowForwardSpeed = 0.9f;

    private async Task SetPositionDecimals() => _celesteService.SetPositionDecimals(
        await _dialogService.ShowIntInputDialogAsync(_celesteService.GetPositionDecimals(), MinDecimals, MaxDecimals, "Set position decimals"));

    private async Task SetSpeedDecimals() => _celesteService.SetSpeedDecimals(
        await _dialogService.ShowIntInputDialogAsync(_celesteService.GetSpeedDecimals(), MinDecimals, MaxDecimals, "Set speed decimals"));

    private async Task SetVelocityDecimals() => _celesteService.SetVelocityDecimals(
        await _dialogService.ShowIntInputDialogAsync(_celesteService.GetVelocityDecimals(), MinDecimals, MaxDecimals, "Set velocity decimals"));

    private async Task SetCustomInfoDecimals() => _celesteService.SetCustomInfoDecimals(
        await _dialogService.ShowIntInputDialogAsync(_celesteService.GetCustomInfoDecimals(), MinDecimals, MaxDecimals, "Set custom info decimals"));

    private async Task SetSubpixelIndicatorDecimals() => _celesteService.SetSubpixelIndicatorDecimals(
        await _dialogService.ShowIntInputDialogAsync(_celesteService.GetSubpixelIndicatorDecimals(), MinDecimals, MaxDecimals, "Set subpixel indicator decimals"));

    private async Task SetFastForwardSpeed() => _celesteService.SetFastForwardSpeed(
        await _dialogService.ShowIntInputDialogAsync(_celesteService.GetFastForwardSpeed(), MinFastForwardSpeed, MaxFastForwardSpeed, "Set fast forward speed"));

    private async Task SetSlowForwardSpeed() => _celesteService.SetSlowForwardSpeed(
        await _dialogService.ShowFloatInputDialogAsync(_celesteService.GetSlowForwardSpeed(), MinSlowForwardSpeed, MaxSlowForwardSpeed, "Set slow forward speed"));

    private async Task<bool> ConfirmDiscardChangesAsync() {
        if (!Document.Dirty) return true;
        bool result = await _dialogService.ShowConfirmDialogAsync("You have unsaved changes. Are you sure?");
        if (result) await Task.Delay(TimeSpan.FromSeconds(0.1f));
        return result;
    }

    private async void NewFile() {
        // delay to allow the UI to recover
        await Task.Delay(TimeSpan.FromSeconds(0.1f));

        if (!await ConfirmDiscardChangesAsync()) return;
        Document = TASDocument.CreateBlank();
    }

    private async void OpenFile() {
        // delay to allow the UI to recover
        await Task.Delay(TimeSpan.FromSeconds(0.1f));

        if (!await ConfirmDiscardChangesAsync()) return;
        string[] results = await _dialogService.ShowOpenFileDialogAsync("Select a CeleseTAS file", _tasFileType);

        if (results?.FirstOrDefault() is not { } filepath) return;

        _celesteService.WriteWait();

        if (TASDocument.Load(filepath) is not { } doc) {
            await _dialogService.ShowDialogAsync($"Error loading file: {filepath}");
            return;
        }

        _settingsService.LastOpenFilePath = filepath;

        if (filepath != null) _celesteService.SendPath(filepath);

        Document = doc;
    }

    private async void SaveFile() {
        // delay to allow the UI to recover
        await Task.Delay(TimeSpan.FromSeconds(0.1f));

        _celesteService.WriteWait();

        await SaveFileAsAsync(false);

        if (Document.FilePath != null) _celesteService.SendPath(Document.FilePath);
    }

    private async void SaveFileAs() {
        // delay to allow the UI to recover
        await Task.Delay(TimeSpan.FromSeconds(0.1f));

        _celesteService.WriteWait();

        await SaveFileAsAsync(true);

        if (Document.FilePath != null) _celesteService.SendPath(Document.FilePath);
    }

    private async Task SaveFileAsAsync(bool force) {
        if (force || Document.FilePath == null) {
            Document.FilePath = await _dialogService.ShowSaveFileDialogAsync("Select a save location", "tas", _tasFileType);
        }
        if (Document.FilePath == null) return;

        Document.Save();

        _settingsService.LastOpenFilePath = Document.FilePath;
    }

    private void Exit() => Application.Current?.DesktopLifetime().Shutdown();
}
