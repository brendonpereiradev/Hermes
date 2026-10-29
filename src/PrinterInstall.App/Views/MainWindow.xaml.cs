using System;
using System.Windows;
using System.Windows.Threading;
using PrinterInstall.App.ViewModels;
using PrinterInstall.Core.Validation;

namespace PrinterInstall.App.Views;

public partial class MainWindow
{
    public MainWindow(MainViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        EnsureWindowFitsScreen();
    }

    /// <summary>
    /// O campo único guardaria só a primeira linha de uma lista colada; com mais de um nome,
    /// o texto vai para o modo lista e o foco segue para a caixa da lista.
    /// </summary>
    private void SingleComputerBox_OnPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (DataContext is not MainViewModel vm || e.DataObject.GetData(DataFormats.UnicodeText) is not string pasted)
            return;

        var box = SingleComputerBox;
        var result = box.Text.Remove(box.SelectionStart, box.SelectionLength).Insert(box.SelectionStart, pasted);
        if (ComputerNameListParser.Parse(result).Count <= 1)
            return;

        e.CancelCommand();
        vm.ComputersText = result;
        Dispatcher.InvokeAsync(() =>
        {
            ListComputersBox.Focus();
            ListComputersBox.CaretIndex = ListComputersBox.Text.Length;
        }, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Garante que a janela caiba completamente dentro da área de trabalho do monitor (WorkArea),
    /// evitando que a barra de título ou navbar fiquem projetadas para fora da tela em notebooks.
    /// </summary>
    private void EnsureWindowFitsScreen()
    {
        var workArea = SystemParameters.WorkArea;

        // Se a janela for maior que a área útil do monitor, redimensiona proporcionalmente
        if (Height > workArea.Height * 0.96)
        {
            Height = Math.Max(MinHeight, workArea.Height * 0.94);
        }

        if (Width > workArea.Width * 0.96)
        {
            Width = Math.Max(MinWidth, workArea.Width * 0.94);
        }

        // Garante que o topo e a esquerda nunca fiquem fora dos limites da tela visível
        if (Top < workArea.Top)
        {
            Top = workArea.Top + 8;
        }

        if (Left < workArea.Left)
        {
            Left = workArea.Left + 8;
        }
    }
}
