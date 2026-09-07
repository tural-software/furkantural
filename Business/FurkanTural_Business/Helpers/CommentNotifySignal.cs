namespace FurkanTural_Business.Helpers;

/// <summary>Yanıt bildirimi turunu erken bitiren tek yönlü işaret; gerekçesi ve sınırları <see cref="NewsletterDispatchSignal"/> ile birebir aynıdır.<para>İki işaretin ayrı tutulması bilinçlidir. Ortak tek bir işaret, bir bülten kuyruğa girdiğinde yorum turunu da uyandırırdı; iki iş birbirinden bağımsız olduğu için gereksiz uyanmalar yalnızca boş sorgu demek olurdu.</para></summary>
public sealed class CommentNotifySignal
{
    private readonly SemaphoreSlim _gate = new(0, 1);

    public void Raise()
    {
        try
        {
            _gate.Release();
        }
        catch (SemaphoreFullException)
        {
        }
    }

    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        => _gate.WaitAsync(timeout, cancellationToken);
}
