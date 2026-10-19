(() => {
    'use strict';
    if (!window.Chart) return;
    const root = document.documentElement;
    const reduced = matchMedia('(prefers-reduced-motion: reduce)');
    const charts = new Map();
    const draw = () => {
        const css = getComputedStyle(root);
        const token = name => css.getPropertyValue(name).trim();
        document.querySelectorAll('[data-report-chart]').forEach(host => {
            charts.get(host)?.destroy();
            host.hidden = false;
            const config = JSON.parse(host.querySelector('[data-chart-config]').textContent);
            const color = tone => tone === 'primary' ? token('--fx-primary') : tone === 'accent' ? token('--fx-accent') :
                getComputedStyle(host.querySelector(`[data-chart-tone="${tone}"]`)).getPropertyValue('--tone-solid').trim();
            const duration = reduced.matches ? 0 : parseFloat(token('--fx-dur-5')) * (token('--fx-dur-5').endsWith('ms') ? 1 : 1000);
            const chart = new Chart(host.querySelector('canvas'), {
                type: config.Type,
                data: { labels: config.Labels, datasets: config.Series.map(series => ({
                    label: series.Label, data: series.Values, borderColor: color(series.Tone),
                    backgroundColor: config.Type === 'line' && series.Tone === 'primary' ? token('--fx-chart-fill') : color(series.Tone),
                    fill: config.Type === 'line' && series.Tone === 'primary', tension: 0.25, borderWidth: 2, pointRadius: 3,
                    pointBackgroundColor: color(series.Tone), maxBarThickness: 48
                })) },
                options: { locale: root.lang, responsive: true, maintainAspectRatio: false,
                    animation: duration ? { duration } : false,
                    plugins: { legend: { labels: { color: token('--fx-ink') } } },
                    scales: { x: { ticks: { color: token('--fx-muted'), maxRotation: 45, autoSkip: true }, grid: { display: false } },
                        y: { beginAtZero: true, ticks: { color: token('--fx-muted') }, grid: { color: token('--fx-border') } } }
                }
            });
            charts.set(host, chart);
        });
    };
    new MutationObserver(draw).observe(root, { attributes: true, attributeFilter: ['data-bs-theme'] });
    reduced.addEventListener('change', draw);
    draw();
})();
