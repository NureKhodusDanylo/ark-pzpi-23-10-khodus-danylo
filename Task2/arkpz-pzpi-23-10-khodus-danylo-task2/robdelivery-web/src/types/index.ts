export interface User {
  id: number;
  userName: string;
  email: string;
  phoneNumber: string;
  address: string;
  role: 'User' | 'Admin' | 'Iot';
  personalNodeId: number;
  photoPath?: string;
}

export interface Order {
  id: number;
  name: string;
  description: string;
  weight: number;
  deliveryPrice: number;
  productPrice: number;
  isProductPaid: boolean;
  deliveryPayer: number;
  deliveryPayerName: string;
  isDeliveryPaid: boolean;
  status: 'Pending' | 'Processing' | 'EnRoute' | 'Delivered' | 'Cancelled';
  senderId: number;
  senderName: string;
  recipientId: number;
  recipientName: string;
  robotId?: number;
  robotName?: string;
  pickupNodeId: number;
  pickupNodeName: string;
  dropoffNodeId: number;
  dropoffNodeName: string;
  createdAt: string;
  completedAt?: string;
  images?: { id: number; fileName: string; filePath: string; contentType: string }[];
}

export interface Robot {
  id: number;
  serialNumber: string;
  type: 'Ground' | 'Aerial';
  status: 'Idle' | 'Delivering' | 'Charging' | 'Maintenance';
  batteryLevel: number;
  currentNodeId?: number;
  currentNode?: Node;
  currentLatitude?: number;
  currentLongitude?: number;
  targetNodeId?: number;
}

export interface Node {
  id: number;
  name: string;
  type: 'UserNode' | 'ChargingStation' | 'Depot';
  latitude: number;
  longitude: number;
  address?: string;
}

export interface AdminStats {
  totalUsers: number;
  totalOrders: number;
  totalRobots: number;
  totalNodes: number;
  activeOrders: number;
  completedOrders: number;
  cancelledOrders: number;
  availableRobots: number;
  busyRobots: number;
  chargingRobots: number;
  averageBatteryLevel: number;
  totalRevenue: number;
  deliveryRevenue: number;
  productRevenue: number;
}

export interface RobotEfficiency {
  robotId: number;
  serialNumber: string;
  completedOrders: number;
  batteryLevel: number;
  efficiencyScore: number;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  userName: string;
  email: string;
  password: string;
  phoneNumber: string;
  latitude: number;
  longitude: number;
  address?: string;
}

export interface AuthResponse {
  token: string;
  status: string;
  message: string;
}

export interface CreateOrderRequest {
  recipientId: number;
  name: string;
  description: string;
  weight: number;
  productPrice: number;
  isProductPaid: boolean;
  deliveryPayer?: number;
  files?: File[];
}

export interface UpdateOrderStatusRequest {
  status: 'Pending' | 'Processing' | 'EnRoute' | 'Delivered' | 'Cancelled';
}
