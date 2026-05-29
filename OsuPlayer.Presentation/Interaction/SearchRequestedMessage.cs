using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Milky.OsuPlayer.Presentation.Interaction;

/// <summary>
/// 用于解耦跨页面检索的事件消息
/// </summary>
public class SearchRequestedMessage : ValueChangedMessage<string>
{
    public SearchRequestedMessage(string value) : base(value)
    {
    }
}