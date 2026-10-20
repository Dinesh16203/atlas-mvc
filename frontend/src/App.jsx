import React,{useCallback,useEffect,useMemo,useState} from 'react';
import { Activity,ArrowRight,AtSign,Eye,EyeOff,LockKeyhole,LogIn} from 'lucide-react';
import { api,post } from './api';
import { startOfWeek,todayIso } from './dates';
import { Sidebar,Header,IconLogo,NotificationsPanel } from './Layout';
import Overview from './Overview';
import TimeEntry from './TimeEntry';
import Approvals from './Approvals';
import Reports from './Reports';
import Administration from './Administration';
import Financials from './Financials';
import { AuditPage,MyWorkPage,ProjectsPage,ResourcePlanningPage,TeamPage,WorkItemsPage } from './Workspaces';

function Login({onLogin}){
  const [email,setEmail]=useState(''),[password,setPassword]=useState(''),[error,setError]=useState(''),[busy,setBusy]=useState(false),[show,setShow]=useState(false);
  const submit=async e=>{e.preventDefault();setBusy(true);setError('');try{await onLogin(email,password)}catch(x){setError(x.message)}finally{setBusy(false)}};
  return <main className="login-screen"><section className="login-brand-side"><div className="login-brand-lockup"><IconLogo/><div><b>ATLAS</b><small>ATRIAS EFFORT INTELLIGENCE</small></div></div><div className="login-brand-copy"><span className="eyebrow">PEOPLE · PROJECTS · PROGRESS</span><h1>Make every hour<br/><em>count.</em></h1><p>One clear view of the work behind every outcome. Log effort, coordinate reviews, and understand where time goes.</p><div className="login-brand-points"><div><span>01</span>Consistent effort capture</div><div><span>02</span>Clear team visibility</div><div><span>03</span>Evidence for better decisions</div></div></div><div className="login-footnote">ATRIAS · HIGHER PERSPECTIVE, BRIGHTER OUTCOMES</div></section><section className="login-panel"><form className="login-card" onSubmit={submit}><div className="mobile-login-brand"><IconLogo/><b>ATLAS</b></div><span className="eyebrow">YOUR WORKSPACE</span><h2>Welcome back</h2><p>Sign in to continue to your ATLAS workspace.</p>{error&&<div className="alert alert-error"><Activity size={15}/>{error}</div>}<label className="field"><span>Email address</span><div className="login-input"><AtSign size={16}/><input type="email" value={email} onChange={e=>setEmail(e.target.value)} placeholder="name@company.com" autoComplete="username" required/></div></label><label className="field"><span>Password</span><div className="login-input"><LockKeyhole size={16}/><input type={show?'text':'password'} value={password} onChange={e=>setPassword(e.target.value)} placeholder="Enter your password" autoComplete="current-password" required/><button type="button" onClick={()=>setShow(!show)} aria-label={show?'Hide password':'Show password'}>{show?<EyeOff size={16}/>:<Eye size={16}/>}</button></div></label><button className="btn btn-primary login-submit" disabled={busy}>{busy?<><span className="spinner spinner-white"/> Signing in…</>:<>Sign in <LogIn size={16}/></>}</button><div className="demo-access"><b>Local demo access</b><span>Use a demo account and password <code>AtlasDemo1!</code></span><button type="button" onClick={()=>{setEmail('member@atlas.local');setPassword('AtlasDemo1!')}}>Member</button><button type="button" onClick={()=>{setEmail('lead@atlas.local');setPassword('AtlasDemo1!')}}>Lead</button><button type="button" onClick={()=>{setEmail('admin@atlas.local');setPassword('AtlasDemo1!')}}>Admin</button><button type="button" onClick={()=>{setEmail('management@atlas.local');setPassword('AtlasDemo1!')}}>Management</button><small>Demo credentials are for localhost only.</small></div><div className="login-help">Corporate SSO is not connected in this local build. Configure your approved Entra ID/OIDC tenant before deployment.</div></form></section></main>;
}

function Boot(){return <div className="boot-screen"><div className="atlas-symbol">A</div><b>ATLAS</b><span className="spinner"/><small>Preparing your workspace</small></div>}

export default function App(){
  const [user,setUser]=useState(null),[lookups,setLookups]=useState(null),[loading,setLoading]=useState(true),[page,setPage]=useState('Home'),[dark,setDark]=useState(localStorage.getItem('atlas-theme')==='dark'),[menuOpen,setMenuOpen]=useState(false),[notificationOpen,setNotificationOpen]=useState(false),[notifications,setNotifications]=useState([]),[week,setWeek]=useState(startOfWeek(todayIso()));
  const refreshLookups=useCallback(async()=>{const d=await api('/api/lookups');setLookups(d);return d},[]);
  const refreshNotifications=useCallback(async()=>{if(!user)return;try{const d=await api('/api/notifications');setNotifications(d.notifications||[])}catch{}},[user]);
  useEffect(()=>{document.body.classList.toggle('dark-theme',dark);localStorage.setItem('atlas-theme',dark?'dark':'light')},[dark]);
  useEffect(()=>{let live=true;api('/api/auth/me').then(async d=>{if(!live)return;setUser(d.user);const l=await api('/api/lookups');if(live)setLookups(l)}).catch(()=>{}).finally(()=>live&&setLoading(false));return()=>{live=false}},[]);
  useEffect(()=>{if(user){refreshNotifications();const id=setInterval(refreshNotifications,60000);return()=>clearInterval(id)}},[user,refreshNotifications]);
  const signIn=async(email,password)=>{const d=await post('/api/auth/login',{email,password});setUser(d.user);setPage('Home');setLookups(await api('/api/lookups'));};
  const signOut=async()=>{await post('/api/auth/logout',{}).catch(()=>{});setUser(null);setLookups(null);setNotifications([]);setPage('Home')};
  const markRead=async id=>{try{await post(`/api/notifications/${id}/read`,{});setNotifications(xs=>xs.map(x=>x.id===id?{...x,read:true}:x))}catch{}};
  const onNavigate=to=>{setPage(to);setNotificationOpen(false);window.scrollTo({top:0,behavior:'smooth'})};
  const pending=notifications.filter(n=>n.kind==='Approval'&&!n.read).length;
  const content=useMemo(()=>{
    if(!user||!lookups)return null;
    switch(page){
      case 'Home': return <Overview user={user} lookups={lookups} notifications={notifications} onNavigate={onNavigate} onNotify={onNavigate} week={week} setWeek={setWeek}/>;
      case 'Time Entry': return <TimeEntry user={user} lookups={lookups} onNavigate={onNavigate} week={week} setWeek={setWeek}/>;
      case 'My Work': return <MyWorkPage user={user} onNavigate={onNavigate}/>;
      case 'Projects & Services': return <ProjectsPage lookups={lookups} onNavigate={onNavigate}/>;
      case 'Work Items': return <WorkItemsPage lookups={lookups} onNavigate={onNavigate}/>;
      case 'Approvals': return <Approvals user={user} lookups={lookups}/>;
      case 'Team': return <TeamPage user={user} lookups={lookups} onNavigate={onNavigate}/>;
      case 'Resource Planning': return <ResourcePlanningPage lookups={lookups}/>;
      case 'Reports': case 'Insights': return <Reports user={user} lookups={lookups} page={page}/>;
      case 'Financials': return <Financials lookups={lookups}/>;
      case 'Audit & Compliance': return <AuditPage user={user}/>;
      case 'Administration': return <Administration lookups={lookups} onRefresh={refreshLookups}/>;
      default: return <Overview user={user} lookups={lookups} notifications={notifications} onNavigate={onNavigate} onNotify={onNavigate} week={week} setWeek={setWeek}/>;
    }
  },[user,lookups,page,notifications,week,onNavigate,refreshLookups]);
  if(loading)return <Boot/>;
  if(!user)return <Login onLogin={signIn}/>;
  if(!lookups)return <div className="boot-screen"><div className="alert alert-error">Could not load ATLAS data. Check that the .NET API is running and refresh this page.</div><button className="btn btn-primary" onClick={()=>location.reload()}>Retry</button></div>;
  return <div className="app-shell"><Sidebar user={user} page={page} onNavigate={onNavigate} open={menuOpen} setOpen={setMenuOpen} approvals={pending}/><main className="app-main"><Header user={user} page={page} onLogout={signOut} dark={dark} onToggleTheme={()=>setDark(x=>!x)} notifications={notifications} onOpenNotifications={()=>{setNotificationOpen(x=>!x);refreshNotifications()}} menuOpen={menuOpen} setMenuOpen={setMenuOpen}/>{notificationOpen&&<NotificationsPanel items={notifications} onClose={()=>setNotificationOpen(false)} onRead={markRead}/>}<div className="main-content">{content}<footer className="app-footer"><span><b>ATLAS</b> · ACTIVITY TRACKING, LOGGING &amp; ANALYTICS SYSTEM</span><span>Local development · v2.0.0</span></footer></div></main></div>;
}
