import React from 'react';
import { Activity, BarChart3, Bell, BriefcaseBusiness, CalendarDays, CheckCircle2, ChevronDown, CircleHelp, ClipboardList, FileBarChart, Gauge, Home, Layers3, LogOut, Menu, Moon, Search, Settings, ShieldCheck, Sun, UsersRound, WalletCards, X } from 'lucide-react';

const links=[
  {name:'Home',icon:Home}, {name:'Time Entry',icon:CalendarDays}, {name:'My Work',icon:ClipboardList},
  {name:'Projects & Services',icon:BriefcaseBusiness}, {name:'Work Items',icon:Layers3},
  {name:'Approvals',icon:CheckCircle2,roles:['Team Lead','Domain Owner','Administrator','Management']},
  {name:'Team',icon:UsersRound,roles:['Team Lead','Domain Owner','Administrator','Management']},
  {name:'Resource Planning',icon:Gauge,roles:['Team Lead','Domain Owner','Administrator','Management']},
  {name:'Reports',icon:FileBarChart},
  {name:'Financials',icon:WalletCards,roles:['Administrator','Management']},
  {name:'Insights',icon:BarChart3},
  {name:'Audit & Compliance',icon:ShieldCheck,roles:['Administrator']},
  {name:'Administration',icon:Settings,roles:['Administrator']},
];

export function Sidebar({user,page,onNavigate,open,setOpen,approvals=0}){
  const visible=links.filter(x=>!x.roles||x.roles.includes(user.role));
  return <><div className={`sidebar-scrim ${open?'visible':''}`} onClick={()=>setOpen(false)}/><aside className={`sidebar ${open?'sidebar-open':''}`}><div className="brand-lockup"><div className="atlas-symbol">A</div><div><b>ATLAS</b><small>Activity Tracking &amp; Insights</small></div><button className="sidebar-close icon-btn" onClick={()=>setOpen(false)} aria-label="Close navigation"><X size={18}/></button></div><div className="org-wordmark"><span>ATRIAS</span><small>PEOPLE · PROJECTS · PROGRESS</small></div><div className="nav-caption">WORKSPACE</div><nav className="side-nav">{visible.map(({name,icon:Icon})=><button key={name} className={`nav-link ${page===name?'selected':''}`} onClick={()=>{onNavigate(name);setOpen(false)}}><Icon size={18} strokeWidth={1.9}/><span>{name}</span>{name==='Approvals'&&approvals>0&&<i className="nav-badge">{approvals}</i>}</button>)}</nav><div className="sidebar-bottom"><div className="support-box"><div className="support-icon"><CircleHelp size={17}/></div><div><b>Need a hand?</b><small>Review your team's guide to logging time.</small></div></div><div className="side-user"><div className="avatar avatar-red">{user.name.split(/\s+/).slice(0,2).map(x=>x[0]).join('').toUpperCase()}</div><div className="side-user-copy"><b>{user.name}</b><small>{user.role}</small></div><span className="live-dot"/></div><div className="side-footer">ATRIAS · BUILT FOR A BRIGHTER TOMORROW</div></div></aside></>;
}

export function Header({user,page,onLogout,dark,onToggleTheme,notifications,onOpenNotifications,menuOpen,setMenuOpen}){
  const unread=notifications.filter(x=>!x.read).length;
  return <header className="topbar"><div className="topbar-left"><button className="icon-btn mobile-menu" onClick={()=>setMenuOpen(!menuOpen)} aria-label="Toggle navigation"><Menu size={19}/></button><div className="top-crumb"><span>ATLAS</span><i>/</i><b>{page}</b></div></div><div className="topbar-right"><label className="global-search"><Search size={16}/><input placeholder="Search people, projects, work items…" aria-label="Search this workspace"/><kbd>⌘ K</kbd></label><div className="header-actions"><button className="icon-btn notification-trigger" aria-label="Notifications" onClick={onOpenNotifications}><Bell size={19}/>{unread>0&&<i>{unread}</i>}</button><button className="icon-btn" aria-label="Help"><CircleHelp size={18}/></button><button className="avatar avatar-header" onClick={onLogout} title="Sign out">{user.name.split(/\s+/).slice(0,2).map(x=>x[0]).join('').toUpperCase()}</button><button className="icon-btn profile-chevron" onClick={onLogout} aria-label="Sign out"><LogOut size={16}/></button><button className="icon-btn theme-toggle" onClick={onToggleTheme} aria-label="Toggle color theme">{dark?<Sun size={17}/>:<Moon size={17}/>}</button></div></div></header>;
}

export function NotificationsPanel({items,onClose,onRead}){
  return <div className="notification-panel"><div className="panel-head"><div><b>Notifications</b><small>Reminders and review activity</small></div><button className="icon-btn" onClick={onClose} aria-label="Close notifications"><X size={17}/></button></div>{items.length?items.map(n=><button className={`notification-item ${n.read?'read':''}`} key={n.id} onClick={()=>onRead(n.id)}><span className={`notification-symbol ${n.kind==='Approval'?'approval':n.kind==='Action'?'action':''}`}><Bell size={15}/></span><span><b>{n.title}</b><small>{n.message}</small><em>{new Date(n.createdAt).toLocaleString()}</em></span>{!n.read&&<i className="unread-dot"/>}</button>):<div className="empty-state compact"><Bell size={22}/><b>You're all caught up</b><span>New reminders and approvals will show here.</span></div>}</div>;
}

export function PageHeading({eyebrow='ATLAS · WORKSPACE',title,subtitle,actions}){
  return <div className="page-heading"><div><div className="eyebrow">{eyebrow}</div><h1>{title}</h1>{subtitle&&<p>{subtitle}</p>}</div>{actions&&<div className="heading-actions">{actions}</div>}</div>;
}

export const IconLogo=()=> <div className="atlas-symbol">A</div>;
