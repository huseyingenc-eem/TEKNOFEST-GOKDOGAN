/* global React */
// ============================================================
// ContactList - Diğer takımların İHA'ları (friendly/hostile)
// TEKNOFEST API'den gelen veriler
// ============================================================

const ContactList = ({ contacts, onLock, lockedId }) => {
  return (
    <div className="hud-panel hud-corners" style={clStyles.wrap}>
      <span className="corner-bl"/><span className="corner-br"/>
      <div style={clStyles.header}>
        <span className="hud-label">▸ HAVA RESMİ</span>
        <div style={{display: 'flex', gap: 6}}>
          <span className="hud-chip red">
            {contacts.filter(c => c.team === 'hostile').length} HASIM
          </span>
          <span className="hud-chip green">
            {contacts.filter(c => c.team === 'friendly').length} DOST
          </span>
        </div>
      </div>

      <div style={clStyles.list}>
        {contacts.map(c => {
          const color = c.team === 'hostile' ? 'var(--danger)' : c.team === 'friendly' ? 'var(--hud-green)' : 'var(--warn)';
          const bright = c.team === 'hostile' ? 'var(--danger-bright)' : c.team === 'friendly' ? 'var(--hud-green)' : 'var(--warn)';
          const isLocked = lockedId === c.id;
          return (
            <div
              key={c.id}
              onClick={() => onLock(c.id)}
              style={{
                ...clStyles.item,
                borderLeft: `2px solid ${color}`,
                background: isLocked ? `rgba(255, 46, 76, 0.12)` : 'transparent',
              }}
            >
              <div style={{display: 'flex', justifyContent: 'space-between', alignItems: 'center'}}>
                <div style={{display: 'flex', gap: 8, alignItems: 'center'}}>
                  <span style={{
                    width: 8, height: 8, borderRadius: '50%',
                    background: color, boxShadow: `0 0 6px ${color}`,
                  }} className={c.team === 'hostile' ? 'pulse' : ''}/>
                  <span className="mono" style={{fontSize: 12, color: '#fff', fontWeight: 600, letterSpacing: '0.06em'}}>
                    {c.id}
                  </span>
                  <span className="mono" style={{fontSize: 10, color: bright, textTransform: 'uppercase'}}>
                    {c.team === 'hostile' ? 'HSM' : c.team === 'friendly' ? 'DST' : 'BLNMZ'}
                  </span>
                </div>
                {isLocked && <span className="hud-chip red" style={{fontSize: 9}}>◎ LOCK</span>}
              </div>
              <div style={{display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 4, marginTop: 6, fontSize: 10, fontFamily: 'var(--font-mono)'}}>
                <Stat label="ALT" value={`${c.alt}m`}/>
                <Stat label="SPD" value={`${c.speed}`}/>
                <Stat label="HDG" value={`${String(c.heading).padStart(3,'0')}°`}/>
                <Stat label="RNG" value={`${c.range}m`}/>
              </div>
            </div>
          );
        })}
      </div>

      <div style={clStyles.footer}>
        <span className="hud-label-dim" style={{fontSize: 9}}>⇅ TEKNOFEST API · Son güncelleme:</span>
        <span className="mono" style={{fontSize: 10, color: 'var(--hud-green)'}}>0.8s ÖNCE</span>
      </div>
    </div>
  );
};

const Stat = ({ label, value }) => (
  <div>
    <span style={{color: 'var(--text-dim)'}}>{label} </span>
    <span style={{color: '#fff'}}>{value}</span>
  </div>
);

const clStyles = {
  wrap: { width: 300, display: 'flex', flexDirection: 'column', maxHeight: '100%' },
  header: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '12px 14px 10px', borderBottom: '1px solid var(--border-dim)' },
  list: { flex: 1, overflowY: 'auto' },
  item: {
    padding: '8px 12px', cursor: 'pointer',
    borderBottom: '1px solid var(--border-dim)',
    transition: 'background 0.15s',
  },
  footer: {
    padding: '8px 12px', display: 'flex', justifyContent: 'space-between',
    borderTop: '1px solid var(--border-dim)', alignItems: 'center',
  },
};

window.ContactList = ContactList;
