using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FurkanTural_Blog.Models;
using FurkanTural_Blog.Services;

namespace FurkanTural_Blog.Controllers;

public class HomeController(IBlogApiService blogApi) : Controller
{
    private readonly IBlogApiService _blogApi = blogApi;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var posts = await _blogApi.GetPostsAsync(cancellationToken);
        return View(posts);
    }

    public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
    {
        var post = await _blogApi.GetPostAsync(id, cancellationToken);
        if (post is null)
            return NotFound();
        return View(post);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? code = null)
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = code
        });
    }
}
