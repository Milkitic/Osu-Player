using System.ComponentModel;
using System.Runtime.CompilerServices;
using Milky.OsuPlayer.Presentation.Annotations;

namespace Milky.OsuPlayer.Presentation.Interaction
{
    /// <summary>
    /// ViewModel基础类
    /// </summary>
    public abstract class VmBase : INotifyPropertyChanged
    {
        /// <summary>在属性值更改时发生。</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 通知UI更新操作
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
