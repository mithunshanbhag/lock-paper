using CommunityToolkit.Mvvm.Input;
using LockPaper.Ui.Models;

namespace LockPaper.Ui.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}