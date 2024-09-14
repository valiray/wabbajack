﻿using System.Reactive.Disposables;
using ReactiveUI;

namespace Wabbajack;

/// <summary>
/// Interaction logic for CompilerFileManagerView.xaml
/// </summary>
public partial class CompilerFileManagerView : ReactiveUserControl<CompilerFileManagerVM>
{
    public CompilerFileManagerView()
    {
        InitializeComponent();


        this.WhenActivated(disposables =>
        {
            this.WhenAny(x => x.ViewModel.Files)
                .BindToStrict(this, v => v.FileTreeView.ItemsSource)
                .DisposeWith(disposables);

            /*
            this.BindStrict(ViewModel, vm => vm.Search, x => x.SearchBox.Text)
                .DisposeWith(dispose);
            */

            this.BindCommand(ViewModel, vm => vm.PrevCommand, v => v.PrevButton)
                .DisposeWith(disposables);

        });

    }

}
