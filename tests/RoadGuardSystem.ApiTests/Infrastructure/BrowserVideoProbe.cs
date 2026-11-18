using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace RoadGuardSystem.ApiTests.Infrastructure;

// Opt-in loopback transport for an interactive browser. Requests still traverse the
// production TestServer authentication, authorization, SQL and MinIO pipeline.
internal static class BrowserVideoProbe
{
    public static async Task RunAsync(AuthenticationWebApplicationFactory factory, string pm, string supervisor,
        Guid fileId, ITestOutputHelper output)
    {
        var completed = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var traces = new ConcurrentQueue<string>();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://localhost:18199");
        builder.Logging.ClearProviders();
        await using var bridge = builder.Build();
        using var upstream = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        var actors = JsonSerializer.Serialize(new[] { pm, supervisor });
        var contentPath = $"/api/v1/files/{fileId}/content";
        bridge.Run(async context =>
        {
            if (context.Request.Path == "/probe/result")
            {
                using var reader = new StreamReader(context.Request.Body);
                completed.TrySetResult(await reader.ReadToEndAsync(context.RequestAborted));
                await context.Response.WriteAsync("Recorded");
                return;
            }
            if (context.Request.Path == "/")
            {
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync("""
                    <!doctype html><meta charset="utf-8"><title>RoadGuard local playback probe</title>
                    <h1>RoadGuard authenticated playback</h1><button id="run">Run PM and Supervisor</button>
                    <video id="video" controls width="640" preload="metadata"></video><pre id="result">Ready</pre>
                    <script>
                    const actors=ACTORS;const path=CONTENT_PATH;
                    const v=document.querySelector('#video'),r=document.querySelector('#result');
                    function event(name){return new Promise((resolve,reject)=>{const timer=setTimeout(()=>reject(Error(name+' timeout')),30000);v.addEventListener(name,()=>{clearTimeout(timer);resolve()}, {once:true});v.addEventListener('error',()=>reject(Error('media '+v.error?.code)),{once:true})})}
                    async function json(url,init={}){const x=await fetch(url,{credentials:'include',...init});if(!x.ok)throw Error(url+' '+x.status);return x.json()}
                    document.querySelector('#run').onclick=async()=>{const results=[];try{for(const email of actors){v.pause();v.removeAttribute('src');v.load();let csrf=await json('/api/v1/auth/web/csrf');if(results.length){const logout=await fetch('/api/v1/auth/web/logout',{method:'POST',credentials:'include',headers:{'X-CSRF-TOKEN':csrf.requestToken,'Idempotency-Key':crypto.randomUUID()}});if(!logout.ok)throw Error('logout '+logout.status);csrf=await json('/api/v1/auth/web/csrf')}await json('/api/v1/auth/web/login',{method:'POST',headers:{'Content-Type':'application/json','X-CSRF-TOKEN':csrf.requestToken},body:JSON.stringify({email,password:'Current1!'})});const loaded=event('loadeddata');v.src=path;await loaded;await v.play();await new Promise(x=>setTimeout(x,600));v.pause();const before=[];for(let i=0;i<v.buffered.length;i++)before.push([v.buffered.start(i),v.buffered.end(i)]);if(before.some(x=>x[0]<=90&&x[1]>=90))throw Error('seek target already buffered');const seek=event('seeked');v.currentTime=90;await seek;await v.play();await new Promise(x=>setTimeout(x,800));if(v.paused||v.currentTime<=90)throw Error('playback did not advance');results.push({role:results.length?'Supervisor':'PM',duration:v.duration,bufferedBefore:before,afterSeek:v.currentTime});r.textContent=JSON.stringify(results,null,2)}v.pause();await fetch('/probe/result',{method:'POST',body:JSON.stringify({pass:true,results})});}catch(e){r.textContent=String(e);await fetch('/probe/result',{method:'POST',body:JSON.stringify({pass:false,error:String(e),results})})}};
                    </script>
                    """.Replace("ACTORS", actors, StringComparison.Ordinal).Replace("CONTENT_PATH", JsonSerializer.Serialize(contentPath), StringComparison.Ordinal));
                return;
            }
            using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Path + context.Request.QueryString);
            if (context.Request.ContentLength > 0) request.Content = new StreamContent(context.Request.Body);
            foreach (var header in context.Request.Headers)
                if (header.Key is not ("Host" or "Content-Length" or "Connection") && !request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                    request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            try
            {
                using var response = await upstream.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
                context.Response.StatusCode = (int)response.StatusCode;
                foreach (var header in response.Headers.Concat(response.Content.Headers))
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                context.Response.Headers.Remove("Transfer-Encoding");
                if (context.Request.Path == contentPath)
                    traces.Enqueue($"Range={context.Request.Headers.Range}; status={(int)response.StatusCode}; Content-Range={response.Content.Headers.ContentRange}; Content-Length={response.Content.Headers.ContentLength}; MIME={response.Content.Headers.ContentType}");
                await using var body = await response.Content.ReadAsStreamAsync(context.RequestAborted);
                var buffer = new byte[16384];
                int read;
                while ((read = await body.ReadAsync(buffer, context.RequestAborted)) > 0)
                {
                    await context.Response.Body.WriteAsync(buffer.AsMemory(0, read), context.RequestAborted);
                    if (context.Request.Path == contentPath) await Task.Delay(8, context.RequestAborted);
                }
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            { traces.Enqueue("Browser cancelled request; upstream response disposed."); }
        });
        await bridge.StartAsync();
        var marker = Environment.GetEnvironmentVariable("RG_BROWSER_READY");
        if (marker is not null) await File.WriteAllTextAsync(marker, "http://localhost:18199");
        try
        {
            var result = await completed.Task.WaitAsync(TimeSpan.FromMinutes(4));
            output.WriteLine(result);
            foreach (var trace in traces) output.WriteLine(trace);
            using var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("pass").GetBoolean(), result);
            Assert.Contains(traces, row => row.Contains("status=206", StringComparison.Ordinal));
        }
        finally { await bridge.StopAsync(); }
    }
}
