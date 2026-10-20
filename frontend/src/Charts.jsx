import React from 'react';

const palette = ['#a90c14', '#d76068', '#ef9a9f', '#868b92', '#e7d2d3', '#6b7380'];

export function BarChart({ data, labelKey = 'label', valueKey = 'value', color = 'var(--atlas-red)' }) {
  const max = Math.max(1, ...data.map(x => Number(x[valueKey] || 0)));
  const height = 184, width = 680, left = 38, right = 10, bottom = 28, top = 14;
  const usableH = height - top - bottom, usableW = width - left - right;
  const n = Math.max(data.length, 1), band = usableW / n, barW = Math.min(34, band * .56);
  return <div className="chart-frame"><svg className="bar-svg" viewBox={`0 0 ${width} ${height}`} role="img" aria-label="Hours entered by day">
    {[0, .33, .66, 1].map((r, i) => <g key={i}><line x1={left} x2={width - right} y1={top + usableH * r} y2={top + usableH * r} className="chart-gridline"/><text x={left - 8} y={top + usableH * r + 3} textAnchor="end" className="chart-tick">{Math.round(max * (1 - r))}</text></g>)}
    {data.map((d, i) => { const val = Number(d[valueKey] || 0), h = val / max * usableH, x = left + band * i + (band - barW) / 2; return <g key={i}><title>{d[labelKey]}: {val.toFixed(1)} hours</title><rect x={x} y={top + usableH - h} width={barW} height={h} rx="4" fill={i === data.length - 1 ? 'var(--atlas-red)' : 'var(--chart-muted)'} /><text x={x + barW / 2} y={Math.max(top + 10, top + usableH - h - 5)} textAnchor="middle" className="chart-value">{val ? val.toFixed(0) : ''}</text><text x={x + barW / 2} y={height - 6} textAnchor="middle" className="chart-tick">{d[labelKey]}</text></g> })}
  </svg></div>;
}

export function LineChart({ data }) {
  const width=680,height=178,left=40,right=8,top=15,bottom=26,max=Math.max(100,...data.map(x=>Number(x.value||0))),min=Math.min(0,...data.map(x=>Number(x.value||0))),span=max-min||1;
  const pts=data.map((d,i)=>({x:left+(width-left-right)*(data.length<2?0:i/(data.length-1)),y:top+(height-top-bottom)*(1-(d.value-min)/span),...d}));
  return <div className="chart-frame"><svg className="bar-svg" viewBox={`0 0 ${width} ${height}`} role="img" aria-label="Utilization trend by week">{[0,.5,1].map((r,i)=><g key={i}><line x1={left} x2={width-right} y1={top+(height-top-bottom)*r} y2={top+(height-top-bottom)*r} className="chart-gridline"/><text x={left-8} y={top+(height-top-bottom)*r+3} textAnchor="end" className="chart-tick">{Math.round(max*(1-r))}%</text></g>)}<polyline points={pts.map(p=>`${p.x},${p.y}`).join(' ')} fill="none" stroke="var(--atlas-red)" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round"/>{pts.map((p,i)=><g key={i}><title>{p.label}: {p.value}%</title><circle cx={p.x} cy={p.y} r="4" fill="var(--atlas-red)"/><text x={p.x} y={height-4} textAnchor="middle" className="chart-tick">{p.label}</text></g>)}</svg></div>;
}

export function Donut({ data, total, suffix = 'Hours' }) {
  const sum = data.reduce((n,x)=>n+Number(x.value||0),0)||1;let acc=0;
  const stops=data.map((d,i)=>{const start=acc/sum*100;acc+=Number(d.value||0);return `${palette[i%palette.length]} ${start}% ${acc/sum*100}%`;}).join(', ');
  return <div className="donut-wrap"><div className="donut" style={{ background: `conic-gradient(${stops})` }}><div className="donut-hole"><strong>{Number(total||0).toLocaleString(undefined,{maximumFractionDigits:0})}</strong><span>{suffix}</span></div></div><div className="donut-legend">{data.map((d,i)=><div className="legend-row" key={d.label}><i style={{ background: palette[i%palette.length] }}/><span>{d.label}</span><b>{sum?Math.round(Number(d.value||0)/sum*100):0}%</b></div>)}</div></div>;
}

export function MiniBars({ data }) { const max=Math.max(1,...data.map(x=>x.value||0));return <div className="mini-bars">{data.map((x,i)=><div key={i} className="mini-bar-column" title={`${x.label}: ${Number(x.value||0).toFixed(1)}h`}><div className="mini-bar-track"><span style={{height:`${Math.max(4,(x.value||0)/max*100)}%`}}/></div><small>{x.label}</small></div>)}</div>; }
