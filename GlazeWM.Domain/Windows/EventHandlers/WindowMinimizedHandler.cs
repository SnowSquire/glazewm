using System.Linq;
using GlazeWM.Domain.Containers;
using GlazeWM.Domain.Containers.Commands;
using GlazeWM.Domain.Workspaces;
using GlazeWM.Infrastructure.Bussing;
using GlazeWM.Infrastructure.WindowsApi.Events;

namespace GlazeWM.Domain.Windows.EventHandlers
{
  class WindowMinimizedHandler : IEventHandler<WindowMinimizedEvent>
  {
    private Bus _bus;
    private WindowService _windowService;
    private ContainerService _containerService;
    private WorkspaceService _workspaceService;

    public WindowMinimizedHandler(Bus bus, WindowService windowService, ContainerService containerService, WorkspaceService workspaceService)
    {
      _bus = bus;
      _windowService = windowService;
      _containerService = containerService;
      _workspaceService = workspaceService;
    }

    public void Handle(WindowMinimizedEvent @event)
    {
      var window = _windowService.GetWindows()
        .FirstOrDefault(window => window.Hwnd == @event.WindowHandle);

      if (window == null)
        return;

      var minimizedWindow = new MinimizedWindow(window.Hwnd, window.OriginalWidth, window.OriginalHeight);

      // Keep reference to the window's ancestor workspace and focus order index prior to detaching.
      var workspace = _workspaceService.GetWorkspaceFromChildContainer(window);

      _bus.Invoke(new DetachContainerCommand(window));
      _bus.Invoke(new AttachContainerCommand(workspace, minimizedWindow));

      _containerService.ContainersToRedraw.Add(workspace);
      _bus.Invoke(new RedrawContainersCommand());
    }
  }
}
