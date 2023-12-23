using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTFO.API.API
{
    /// <summary>
    /// Handle that allocated when you called <see cref="AssetAPI.WantToWorkForStartupAssets"/>
    /// This blocks game loading until every allocated AssetLoadHandle as marked as completed
    /// </summary>
    public sealed class AssetLoadHandle
    {
        /// <summary>
        /// true if current load job has ended
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// Mark this specific Asset loading job as completed
        /// </summary>
        public void SetCompleted()
        {
            IsCompleted = true;
        }

        /// <summary>
        /// Add Line to loading text 
        /// </summary>
        public void AddLoadingText(string text)
        {
            MainMenuGuiLayer.Current.PageIntro.m_textCenter.AddLine($"<size=35%>{text}</size>");
        }
    }
}
