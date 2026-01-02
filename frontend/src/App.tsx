import { useEffect, useState } from 'react';
import axios from 'axios';
import { 
  Grid, Card, CardContent, Typography, 
  Chip, Table, TableHead, TableRow, TableCell, TableBody, 
  Box, AppBar, Toolbar, IconButton, Paper, CssBaseline, ThemeProvider, createTheme, Badge, LinearProgress
} from '@mui/material';
import { 
  Factory, Warning, CheckCircle, Error as ErrorIcon, 
  PrecisionManufacturing, NotificationsActive, Wifi, Settings 
} from '@mui/icons-material';

// Import Types
import { MachineStatus, AlertSeverity } from './types';
import type { Machine, Alert } from './types';

// ---------------------------------------------------------------------------
// 🔧 CONFIGURATION FIX: Port 5182 matches your .NET launchSettings.json
// ---------------------------------------------------------------------------
const API_URL = 'http://localhost:5182'; 

// 🛑 CUSTOM DARK THEME
const darkTheme = createTheme({
  palette: {
    mode: 'dark',
    primary: { main: '#00e5ff' }, 
    secondary: { main: '#ff4081' }, 
    background: {
      default: '#050b14', 
      paper: '#0a1929', 
    },
    text: { primary: '#e0f7fa', secondary: '#b0bec5' }
  },
  typography: {
    fontFamily: '"Inter", "Roboto", "Helvetica", "Arial", sans-serif',
    h5: { fontWeight: 700, letterSpacing: '0.05rem' },
    h6: { fontWeight: 600 }
  },
  components: {
    MuiPaper: {
      styleOverrides: {
        root: { backgroundImage: 'none' } 
      }
    },
    MuiTableCell: {
      styleOverrides: {
        root: { borderBottom: '1px solid rgba(0, 229, 255, 0.1)' }
      }
    }
  }
});

function App() {
  const [machines, setMachines] = useState<Machine[]>([]);
  const [alerts, setAlerts] = useState<Alert[]>([]);
  const [lastUpdated, setLastUpdated] = useState<Date>(new Date());
  const [loading, setLoading] = useState(true);
  const [connectionError, setConnectionError] = useState(false);

  // POLLING LOOP
  useEffect(() => {
    const fetchData = async () => {
      try {
        const machineRes = await axios.get<Machine[]>(`${API_URL}/machines`);
        const alertRes = await axios.get<Alert[]>(`${API_URL}/alerts`);
        
        const sortedAlerts = alertRes.data.sort((a, b) => 
          new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime()
        );

        setMachines(machineRes.data);
        setAlerts(sortedAlerts);
        setLastUpdated(new Date());
        setLoading(false);
        setConnectionError(false);
      } catch (error) {
        console.error("Connection Lost", error);
        setConnectionError(true);
        setLoading(false);
      }
    };

    fetchData(); 
    const interval = setInterval(fetchData, 2000); 
    return () => clearInterval(interval);
  }, []);

  // 🎨 STATUS HELPERS
  const getStatusColor = (status: MachineStatus) => {
    switch (status) {
      case MachineStatus.Online: return 'success';
      case MachineStatus.Error: return 'error';
      case MachineStatus.Maintenance: return 'warning';
      default: return 'default';
    }
  };

  return (
    <ThemeProvider theme={darkTheme}>
      <CssBaseline />
      
      {/* FULL SCREEN WRAPPER */}
      <Box sx={{ 
        display: 'flex', 
        flexDirection: 'column', 
        height: '100vh', // Forces full viewport height
        width: '100vw',  // Forces full viewport width
        bgcolor: 'background.default',
        overflow: 'hidden' // Prevents double scrollbars
      }}>
        
        {/* 1. TOP NAVIGATION BAR */}
        <AppBar position="static" elevation={0} sx={{ borderBottom: '1px solid rgba(0,229,255,0.2)', bgcolor: '#050b14' }}>
          <Toolbar sx={{ height: 70 }}>
            <Factory sx={{ mr: 2, color: 'primary.main', fontSize: 32 }} />
            <Box sx={{ flexGrow: 1 }}>
              <Typography variant="h5" component="div" sx={{ color: 'primary.main', lineHeight: 1 }}>
                SWARM<span style={{ color: 'white' }}>FACTORY</span>
              </Typography>
              <Typography variant="caption" sx={{ color: 'text.secondary', letterSpacing: 1 }}>
                ENTERPRISE DIGITAL TWIN PLATFORM
              </Typography>
            </Box>
            
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 3 }}>
              <Box sx={{ textAlign: 'right', display: { xs: 'none', md: 'block' } }}>
                <Typography variant="caption" display="block" color="text.secondary">SYSTEM STATUS</Typography>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  {connectionError ? (
                    <>
                      <Wifi sx={{ fontSize: 14, color: 'error.main' }} />
                      <Typography variant="body2" color="error.main" fontWeight="bold">OFFLINE</Typography>
                    </>
                  ) : (
                    <>
                      <Wifi sx={{ fontSize: 14, color: 'success.main' }} />
                      <Typography variant="body2" color="success.main" fontWeight="bold">CONNECTED</Typography>
                    </>
                  )}
                </Box>
              </Box>
              
              <IconButton color="primary" sx={{ border: '1px solid rgba(0,229,255,0.3)' }}>
                 <Badge badgeContent={alerts.length} color="error">
                   <NotificationsActive />
                 </Badge>
              </IconButton>
              <IconButton color="primary">
                 <Settings />
              </IconButton>
            </Box>
          </Toolbar>
        </AppBar>

        {loading && <LinearProgress color="primary" />}
        {connectionError && <LinearProgress color="error" />}

        {/* 2. MAIN CONTENT AREA */}
        <Box sx={{ flexGrow: 1, p: 3, overflow: 'auto' }}>
          <Grid container spacing={3} sx={{ height: '100%' }}>
            
            {/* LEFT COLUMN: FLEET STATUS */}
            <Grid size={{ xs: 12, md: 8, lg: 9 }}>
              <Paper 
                elevation={0} 
                sx={{ 
                  borderRadius: 3, 
                  border: '1px solid rgba(0,229,255,0.1)',
                  height: '100%',
                  overflow: 'hidden',
                  display: 'flex',
                  flexDirection: 'column'
                }}
              >
                <Box sx={{ 
                  p: 2, 
                  bgcolor: 'rgba(0,229,255,0.05)', 
                  borderBottom: '1px solid rgba(0,229,255,0.1)',
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center'
                }}>
                   <Typography variant="h6" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <PrecisionManufacturing color="primary" /> Manufacturing Fleet
                   </Typography>
                   <Chip label={`${machines.length} Units`} size="small" variant="outlined" color="primary" />
                </Box>

                <Box sx={{ overflowX: 'auto', flexGrow: 1 }}>
                  <Table stickyHeader>
                    <TableHead>
                      <TableRow>
                        <TableCell sx={{ bgcolor: '#0a1929', color: 'text.secondary' }}>ID / NAME</TableCell>
                        <TableCell sx={{ bgcolor: '#0a1929', color: 'text.secondary' }}>TYPE</TableCell>
                        <TableCell sx={{ bgcolor: '#0a1929', color: 'text.secondary' }}>LOCATION</TableCell>
                        <TableCell sx={{ bgcolor: '#0a1929', color: 'text.secondary' }}>LIVE STATUS</TableCell>
                        <TableCell sx={{ bgcolor: '#0a1929', color: 'text.secondary' }}>MAINTENANCE</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {machines.length === 0 ? (
                        <TableRow>
                          <TableCell colSpan={5} align="center" sx={{ py: 8 }}>
                            {connectionError ? (
                              <Typography variant="h6" color="error">
                                ⚠️ Cannot connect to TwinAPI on port 5182. <br/> Is the backend running?
                              </Typography>
                            ) : (
                              <>
                                <Typography variant="h6" color="text.secondary">Waiting for Telemetry Stream...</Typography>
                                <Typography variant="body2" color="text.secondary">Start the Simulator to view fleet data.</Typography>
                              </>
                            )}
                          </TableCell>
                        </TableRow>
                      ) : (
                        machines.map((m) => (
                          <TableRow key={m.id} hover sx={{ '&:hover': { bgcolor: 'rgba(0,229,255,0.04)' } }}>
                            <TableCell>
                              <Typography variant="body1" fontWeight="bold" color="white">{m.name}</Typography>
                              <Typography variant="caption" sx={{ fontFamily: 'monospace', opacity: 0.5 }}>{m.id.split('-')[0]}</Typography>
                            </TableCell>
                            <TableCell>{m.type}</TableCell>
                            <TableCell>{m.location}</TableCell>
                            <TableCell>
                              <Chip 
                                icon={m.status === 'Online' ? <CheckCircle/> : <ErrorIcon/>} 
                                label={m.status.toUpperCase()} 
                                color={getStatusColor(m.status)} 
                                size="small" 
                                variant="filled"
                                sx={{ fontWeight: 'bold' }}
                              />
                            </TableCell>
                            <TableCell sx={{ fontFamily: 'monospace' }}>
                                {new Date(m.lastMaintenanceDate).toLocaleDateString()}
                            </TableCell>
                          </TableRow>
                        ))
                      )}
                    </TableBody>
                  </Table>
                </Box>
              </Paper>
            </Grid>

            {/* RIGHT COLUMN: ALERT STREAM */}
            <Grid size={{ xs: 12, md: 4, lg: 3 }}>
              <Paper 
                elevation={0} 
                sx={{ 
                  borderRadius: 3, 
                  height: '100%', 
                  bgcolor: 'rgba(255, 64, 129, 0.05)', 
                  border: '1px solid rgba(255, 64, 129, 0.2)',
                  display: 'flex',
                  flexDirection: 'column'
                }}
              >
                <Box sx={{ p: 2, borderBottom: '1px solid rgba(255, 64, 129, 0.2)' }}>
                   <Typography variant="h6" sx={{ display: 'flex', alignItems: 'center', gap: 1, color: 'secondary.main' }}>
                      <Warning /> Priority Alerts
                   </Typography>
                </Box>

                <Box sx={{ p: 2, flexGrow: 1, overflowY: 'auto', maxHeight: 'calc(100vh - 200px)' }}>
                  {alerts.length === 0 ? (
                    <Box sx={{ textAlign: 'center', py: 6, opacity: 0.5 }}>
                      <CheckCircle sx={{ fontSize: 60, color: 'success.main', mb: 2 }} />
                      <Typography variant="h6" color="success.main">System Nominal</Typography>
                      <Typography variant="caption">No active threats detected.</Typography>
                    </Box>
                  ) : (
                    alerts.map((alert) => (
                      <Card 
                        key={alert.id} 
                        elevation={2}
                        sx={{ 
                          mb: 2, 
                          bgcolor: '#0a1929', 
                          borderLeft: `4px solid ${alert.severity === 'High' ? '#f44336' : '#ff9800'}`,
                          transition: 'all 0.2s',
                          '&:hover': { transform: 'translateX(4px)', boxShadow: 4 }
                        }}
                      >
                        <CardContent sx={{ p: 2, '&:last-child': { pb: 2 } }}>
                          <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                             <Chip 
                               label={alert.severity} 
                               color={alert.severity === 'High' ? 'error' : 'warning'} 
                               size="small" 
                               sx={{ height: 20, fontSize: '0.7rem' }}
                             />
                             <Typography variant="caption" color="text.secondary" sx={{ fontFamily: 'monospace' }}>
                               {new Date(alert.timestamp).toLocaleTimeString()}
                             </Typography>
                          </Box>
                          <Typography variant="body2" sx={{ fontWeight: 'bold', mb: 0.5 }}>
                            {alert.message}
                          </Typography>
                          <Typography variant="caption" color="primary.main">
                             UNIT: {alert.machineId.split('-')[0]}
                          </Typography>
                        </CardContent>
                      </Card>
                    ))
                  )}
                </Box>
              </Paper>
            </Grid>

          </Grid>
        </Box>
      </Box>
    </ThemeProvider>
  );
}

export default App;