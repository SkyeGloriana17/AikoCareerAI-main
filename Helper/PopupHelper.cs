using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CareerAI.Helper;
public static class PopupHelper
{
    // success, error
    public static IHtmlContent Popup(this IHtmlHelper htmlHelper, string type, string message)
    {
        string capitalizedType = char.ToUpper(type[0]) + type.Substring(1);
        var popupHtml = $@"
            <div class='popup-{type}' style='position:absolute;top: 20px; left: 50%; transform: translateX(-50%)'>
                <div class='message-popup'>
                    <img src='/assets/{type}.svg' alt='{capitalizedType}' class='icon'>
                    <div class='message-{type}'>
                        <h1>{capitalizedType}!</h1>
                        <p>{message}</p>
                    </div>
                </div>
                <div class='close-popup-{type}' id='close-popup'>
                    <button class='close-button-{type}'>CLOSE</button>
                </div>
            </div>";

        return new HtmlString(popupHtml);
    }
}