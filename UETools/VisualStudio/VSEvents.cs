using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Task = System.Threading.Tasks.Task;

namespace UETools.VisualStudio
{
    public class VSEvents : IDisposable, IVsSelectionEvents, IVsSolutionEvents, IVsUpdateSolutionEvents
    {
        public delegate void OnSolutionOpenedDelegate();
        public event OnSolutionOpenedDelegate OnSolutionOpened;

        public delegate void OnSolutionClosedDelegate();
        public event OnSolutionClosedDelegate OnSolutionClosed;

        public delegate void OnStartupProjectChangedDelegate(Project project);
        public event OnStartupProjectChangedDelegate OnStartupProjectChanged;

        public static VSEvents Instance { get; private set; }

        public IVsMonitorSelection SelectionManager { get; private set; }
        private uint SelectionManagerEventsHandle;

        public IVsSolution2 SolutionManager { get; private set; }
        private uint SolutionManagerEventsHandle;

        public IVsSolutionBuildManager2 SolutionBuildManager { get; private set; }
        private uint SolutionBuildManagerEventsHandle;

        private VSEvents()
        {
        }

        public static void InstantiateSingleton()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            if (Instance != null)
                throw new ApplicationException("Instance already exists!");

            Instance = new VSEvents();

            Instance.SelectionManager = ServiceProvider.GlobalProvider?.GetService(typeof(SVsSolutionBuildManager)) as IVsMonitorSelection;
            Instance.SelectionManager?.AdviseSelectionEvents(Instance, out Instance.SelectionManagerEventsHandle);

            Instance.SolutionManager = ServiceProvider.GlobalProvider?.GetService(typeof(SVsSolution)) as IVsSolution2;
            Instance.SolutionManager?.AdviseSolutionEvents(Instance, out Instance.SolutionManagerEventsHandle);

            Instance.SolutionBuildManager = ServiceProvider.GlobalProvider?.GetService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager2;
            Instance.SolutionBuildManager?.AdviseUpdateSolutionEvents(Instance, out Instance.SolutionBuildManagerEventsHandle);
        }

        /////////////////////////////////////////////////////////////////////////////////
        // Implementation: IDisposable
        void IDisposable.Dispose()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            SelectionManager?.UnadviseSelectionEvents(SelectionManagerEventsHandle);
            SolutionManager?.UnadviseSolutionEvents(SolutionManagerEventsHandle);
            SolutionBuildManager?.UnadviseUpdateSolutionEvents(SolutionBuildManagerEventsHandle);
        }

        /////////////////////////////////////////////////////////////////////////////////
        // Implementation: IVsSelectionEvents
        int IVsSelectionEvents.OnSelectionChanged(IVsHierarchy pHierOld, uint itemidOld, IVsMultiItemSelect pMISOld, ISelectionContainer pSCOld, IVsHierarchy pHierNew, uint itemidNew, IVsMultiItemSelect pMISNew, ISelectionContainer pSCNew)
        {
            return VSConstants.S_OK;
        }

        int IVsSelectionEvents.OnElementValueChanged(uint elementid, object varValueOld, object varValueNew)
        {
            System.Windows.Threading.Dispatcher.CurrentDispatcher.VerifyAccess();

            // Handle startup project changes
            if (elementid == (uint)VSConstants.VSSELELEMID.SEID_StartupProject)
            {
                Project project = Helper.VSHelper.HierarchyObjectToProject((IVsHierarchy)varValueNew);
                if (OnStartupProjectChanged != null)
                {
                    OnStartupProjectChanged(project);
                }
            }

            return VSConstants.S_OK;
        }

        int IVsSelectionEvents.OnCmdUIContextChanged(uint dwCmdUICookie, int fActive)
        {
            return VSConstants.S_OK;
        }

        /////////////////////////////////////////////////////////////////////////////////
        // Implementation: IVsSolutionEvents
        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            if (OnSolutionOpened != null)
            {
                OnSolutionOpened();
            }

            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved)
        {
            if (OnSolutionClosed != null)
            {
                OnSolutionClosed();
            }

            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
                return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        /////////////////////////////////////////////////////////////////////////////////
        // Implementation: IVsUpdateSolutionEvents
        int IVsUpdateSolutionEvents.UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Cancel()
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            return VSConstants.S_OK;
        }
    }
}
