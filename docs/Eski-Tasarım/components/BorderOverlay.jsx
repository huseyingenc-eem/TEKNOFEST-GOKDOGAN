/* global React */
// ============================================================
// BorderOverlay - Harita üstünde uçuş sınırı + HSS + kamikaze alanı
// TEKNOFEST Savaşan İHA sahası standart unsurları
// ============================================================

const BorderOverlay = ({ show }) => {
  if (!show) return null;

  return (
    <svg style={{position: 'absolute', inset: 0, width: '100%', height: '100%', pointerEvents: 'none', zIndex: 5}}
         viewBox="0 0 100 100" preserveAspectRatio="none">
      <defs>
        <pattern id="hatchDanger" patternUnits="userSpaceOnUse" width="3" height="3" patternTransform="rotate(45)">
          <line x1="0" y1="0" x2="0" y2="3" stroke="#ff2e4c" strokeWidth="0.6" opacity="0.4"/>
        </pattern>
        <pattern id="hatchWarn" patternUnits="userSpaceOnUse" width="3" height="3" patternTransform="rotate(45)">
          <line x1="0" y1="0" x2="0" y2="3" stroke="#ffb800" strokeWidth="0.5" opacity="0.35"/>
        </pattern>
        <pattern id="hatchKam" patternUnits="userSpaceOnUse" width="2.5" height="2.5" patternTransform="rotate(-45)">
          <line x1="0" y1="0" x2="0" y2="2.5" stroke="#ff6b1a" strokeWidth="0.4" opacity="0.5"/>
        </pattern>
      </defs>

      {/* Uçuş sahası (dış sınır) */}
      <polygon
        points="12,18 82,15 92,55 78,88 22,92 8,60"
        fill="none" stroke="#ff2e4c" strokeWidth="0.35" strokeDasharray="1.5 0.8"
        vectorEffect="non-scaling-stroke" style={{strokeWidth: 2.5}}
      />
      {/* İnce tampon bölge */}
      <polygon
        points="14,20 80,17 90,55 76,86 24,90 10,60"
        fill="url(#hatchWarn)" opacity="0.8"
      />
      <polygon
        points="14,20 80,17 90,55 76,86 24,90 10,60"
        fill="none" stroke="#ffb800" strokeWidth="0.2" vectorEffect="non-scaling-stroke"
        style={{strokeWidth: 1.2, strokeDasharray: '2 1'}}
      />

      {/* HSS (Hava Savunma Sistemi) bölgeleri - kırmızı daireler */}
      <g>
        <circle cx="65" cy="35" r="10" fill="url(#hatchDanger)" opacity="0.7"/>
        <circle cx="65" cy="35" r="10" fill="none" stroke="#ff2e4c" strokeWidth="0.4"
                vectorEffect="non-scaling-stroke" style={{strokeWidth: 2}}/>
        <circle cx="65" cy="35" r="10" fill="none" stroke="#ff2e4c" strokeWidth="0.2"
                strokeDasharray="1 0.8" vectorEffect="non-scaling-stroke" style={{strokeWidth: 1}}>
          <animate attributeName="r" from="10" to="12" dur="2s" repeatCount="indefinite"/>
          <animate attributeName="opacity" from="0.8" to="0" dur="2s" repeatCount="indefinite"/>
        </circle>
        <text x="65" y="36" fill="#ff2e4c" fontSize="2.2" fontFamily="monospace"
              textAnchor="middle" fontWeight="700">HSS-01</text>
      </g>
      <g>
        <circle cx="30" cy="68" r="8" fill="url(#hatchDanger)" opacity="0.7"/>
        <circle cx="30" cy="68" r="8" fill="none" stroke="#ff2e4c" strokeWidth="0.4"
                vectorEffect="non-scaling-stroke" style={{strokeWidth: 2}}/>
        <text x="30" y="69" fill="#ff2e4c" fontSize="2" fontFamily="monospace"
              textAnchor="middle" fontWeight="700">HSS-02</text>
      </g>

      {/* Kamikaze hedef bölgesi (turuncu X işaretli) */}
      <g>
        <rect x="44" y="48" width="14" height="10" fill="url(#hatchKam)" opacity="0.6"/>
        <rect x="44" y="48" width="14" height="10" fill="none" stroke="#ff6b1a"
              strokeWidth="0.35" vectorEffect="non-scaling-stroke" style={{strokeWidth: 2, strokeDasharray: '2 1'}}/>
        <line x1="44" y1="48" x2="58" y2="58" stroke="#ff6b1a" strokeWidth="0.2"
              vectorEffect="non-scaling-stroke" style={{strokeWidth: 1}} opacity="0.5"/>
        <line x1="58" y1="48" x2="44" y2="58" stroke="#ff6b1a" strokeWidth="0.2"
              vectorEffect="non-scaling-stroke" style={{strokeWidth: 1}} opacity="0.5"/>
        <text x="51" y="46" fill="#ff6b1a" fontSize="2.2" fontFamily="monospace"
              textAnchor="middle" fontWeight="700">KAMİKAZE HEDEF</text>
      </g>

      {/* Sınır etiketleri */}
      <text x="50" y="13" fill="#ff2e4c" fontSize="2" fontFamily="monospace"
            textAnchor="middle" fontWeight="700" letterSpacing="0.3">◤ UÇUŞ SAHASI SINIRI ◥</text>
    </svg>
  );
};

// Legend (sağ alt, harita ekranı için)
const BorderLegend = () => (
  <div className="hud-panel hud-corners" style={{
    padding: '8px 12px', fontFamily: 'var(--font-mono)', fontSize: 10,
  }}>
    <span className="corner-bl"/><span className="corner-br"/>
    <div className="hud-label" style={{fontSize: 9, marginBottom: 6}}>▸ SAHA</div>
    <LegendRow color="#ff2e4c" label="UÇUŞ SINIRI"/>
    <LegendRow color="#ffb800" label="TAMPON BÖLGE"/>
    <LegendRow color="#ff2e4c" label="HSS BÖLGESİ" hatch/>
    <LegendRow color="#ff6b1a" label="KAMİKAZE HEDEF"/>
    <div style={{height: 1, background: 'var(--border-dim)', margin: '6px 0'}}/>
    <LegendRow color="#ff2e4c" label="HASIM İHA" dot/>
    <LegendRow color="#39ff7a" label="DOST İHA" dot/>
    <LegendRow color="#ff6b1a" label="GÖKDOĞAN-01" dot/>
  </div>
);

const LegendRow = ({ color, label, hatch, dot }) => (
  <div style={{display: 'flex', alignItems: 'center', gap: 8, padding: '2px 0'}}>
    <span style={{
      width: 14, height: 10,
      background: hatch ? `repeating-linear-gradient(45deg, ${color}66 0, ${color}66 2px, transparent 2px, transparent 4px)` : 'transparent',
      border: dot ? 'none' : `1.5px solid ${color}`,
      borderRadius: dot ? '50%' : 0,
      ...(dot ? { width: 8, height: 8, background: color, boxShadow: `0 0 4px ${color}` } : {}),
    }}/>
    <span style={{color: '#ccc'}}>{label}</span>
  </div>
);

window.BorderOverlay = BorderOverlay;
window.BorderLegend = BorderLegend;
