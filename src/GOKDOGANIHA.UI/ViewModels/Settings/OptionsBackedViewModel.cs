using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GOKDOGANIHA.UI.ViewModels.Settings;

/// <summary>
/// Form state'i tek bir strongly-typed Options POCO'su ile two-way sync tutan
/// sub-VM'ler için ortak temel. Parametresiz constructor design-time (XAML
/// designer, tests) içindir — <see cref="Options"/> null kalır ve
/// <see cref="PushToOptions"/> sessizce no-op yapar. Parametreli constructor
/// runtime içindir.
/// </summary>
public abstract partial class OptionsBackedViewModel<TOptions> : ObservableObject
    where TOptions : class
{
    protected TOptions? Options { get; }

    protected OptionsBackedViewModel() { }
    protected OptionsBackedViewModel(TOptions options) { Options = options; }

    /// <summary>
    /// Partial <c>OnXxxChanged</c> callback'lerinden çağrılır. Options bağlı
    /// değilse (design-time / test) sessizce atlar.
    /// </summary>
    protected void PushToOptions(Action<TOptions> push)
    {
        if (Options is not null) push(Options);
    }
}
