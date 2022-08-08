namespace Milki.OsuPlayer.Wpf.Command;

/// <summary>
/// The definition of the converter used to convert an EventArgs
/// in the <see cref="EventToCommand"/> class, if the
/// <see cref="EventToCommand.PassEventArgsToCommand"/> property is true.
/// Set an instance of this class to the <see cref="EventToCommand.EventArgsConverter"/>
/// property of the EventToCommand instance.
/// </summary>
////[ClassInfo(typeof(EventToCommand))]
public interface IEventArgsConverter
{
    object Convert(object value, object parameter);
}