using System;
using System.Collections.Generic;
using BeauUtil.Debugger;

namespace Aqua
{
    public class PauseService : ServiceBehaviour
    {
        [NonSerialized] private int m_PauseCount;

        private readonly HashSet<IPauseable> m_PausedServices = new HashSet<IPauseable>();

        public void Pause()
        {
            if (++m_PauseCount == 1)
            {
                PauseImpl();
            }
        }

        public void Resume()
        {   
            Assert.True(m_PauseCount > 0, "PauseService Pause/Resume calls are unbalanced");
            if (--m_PauseCount == 0)
            {
                ResumeImpl();
            }
        }

        private void PauseImpl()
        {
            foreach(var service in Services.AllPauseable())
            {
                if (!service.IsPaused())
                {
                    service.Pause();
                    m_PausedServices.Add(service);
                }
            }
        }

        private void ResumeImpl()
        {
            foreach(var service in m_PausedServices)
            {
                Assert.True(service.IsPaused(), "Service {0} resumed from somewhere else than PauseService");
                service.Resume();
            }
            m_PausedServices.Clear();
        }
    }
}