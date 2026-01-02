// 1. Define Values (Runtime Objects)
export const MachineStatus = {
  Online: 'Online',
  Offline: 'Offline',
  Maintenance: 'Maintenance',
  Error: 'Error'
} as const;

export const AlertSeverity = {
  Low: 'Low',
  Medium: 'Medium',
  High: 'High',
  Critical: 'Critical'
} as const;

// 2. Extract Types from those Values
// This creates a type that equals "Online" | "Offline" | ...
export type MachineStatus = (typeof MachineStatus)[keyof typeof MachineStatus];
export type AlertSeverity = (typeof AlertSeverity)[keyof typeof AlertSeverity];

// 3. Define Interfaces (Pure Types)
export interface Machine {
  id: string;
  name: string;
  type: string;
  status: MachineStatus;
  location: string;
  lastMaintenanceDate: string;
}

export interface Alert {
  id: string;
  machineId: string;
  severity: AlertSeverity;
  message: string;
  timestamp: string;
  status: string;
}