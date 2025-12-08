import axios from 'axios';
import type {
  LoginRequest,
  RegisterRequest,
  AuthResponse,
  User,
  Order,
  CreateOrderRequest,
  UpdateOrderStatusRequest,
  Robot,
  Node,
  AdminStats,
  RobotEfficiency,
} from '../types';

const API_BASE_URL = 'https://ark-pzpi-23-10-khodus-danylo.onrender.com/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Auth APIs
export const authAPI = {
  login: (data: LoginRequest) =>
    api.post<AuthResponse>('/Auth/login', {
      email: data.email,
      password: data.password,
    }),

  register: (data: RegisterRequest) =>
    api.post<AuthResponse>('/Auth/register', {
      userName: data.userName,
      email: data.email,
      password: data.password,
      phoneNumber: data.phoneNumber,
      latitude: data.latitude,
      longitude: data.longitude,
      address: data.address,
    }),

  getCurrentUser: () =>
    api.get<User>('/User/profile'),

  updateProfile: (formData: FormData) =>
    api.put<User>('/User/profile', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }),
};

// User APIs
export const userAPI = {
  getAllUsers: () =>
    api.get<User[]>('/User'),

  searchUsers: (query: string) =>
    api.get<User[]>(`/User/search?query=${query}`),
};

// Order APIs
export const orderAPI = {
  // Admin only - get all orders
  getAllOrders: () =>
    api.get<Order[]>('/Order'),

  // Admin only - get order by ID
  getOrderById: (id: number) =>
    api.get<Order>(`/Order/${id}`),

  // Get current user's orders (sent and received)
  getMyOrders: () =>
    api.get<Order[]>('/Order/my-orders'),

  createOrder: (data: CreateOrderRequest) => {
    const formData = new FormData();
    formData.append('name', data.name);
    formData.append('description', data.description);
    formData.append('weight', data.weight.toString());
    formData.append('productPrice', data.productPrice.toString());
    formData.append('isProductPaid', data.isProductPaid.toString());
    formData.append('recipientId', data.recipientId.toString());
    formData.append('deliveryPayer', (data.deliveryPayer || 0).toString());

    if (data.files && data.files.length > 0) {
      data.files.forEach((file) => {
        formData.append('files', file);
      });
    }

    return api.post<Order>('/Order', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  updateOrderStatus: (id: number, data: UpdateOrderStatusRequest) =>
    api.put<Order>(`/Order/${id}/status`, data),

  assignRobot: (orderId: number, robotId: number) =>
    api.post<Order>(`/Order/${orderId}/assign/${robotId}`),

  cancelOrder: (id: number) =>
    api.post<Order>(`/Order/${id}/cancel`),

  executeOrder: (id: number) =>
    api.post(`/Order/${id}/execute`),

  deleteOrder: (id: number) =>
    api.delete(`/Order/${id}`),
};

// Robot APIs
export const robotAPI = {
  getAllRobots: () =>
    api.get<Robot[]>('/Robot'),

  getRobotById: (id: number) =>
    api.get<Robot>(`/Robot/${id}`),

  getAvailableRobots: () =>
    api.get<Robot[]>('/Robot/available'),

  getRobotsByStatus: (status: string) =>
    api.get<Robot[]>(`/Robot/status/${status}`),

  getRobotsByType: (type: string) =>
    api.get<Robot[]>(`/Robot/type/${type}`),

  createRobot: (data: Partial<Robot>) =>
    api.post<Robot>('/Robot', data),

  updateRobot: (id: number, data: Partial<Robot>) =>
    api.put<Robot>(`/Robot/${id}`, data),

  deleteRobot: (id: number) =>
    api.delete(`/Robot/${id}`),
};

// Node APIs
export const nodeAPI = {
  getAllNodes: () =>
    api.get<Node[]>('/Node'),

  getNodeById: (id: number) =>
    api.get<Node>(`/Node/${id}`),

  getNodesByType: (type: string) =>
    api.get<Node[]>(`/Node/type/${type}`),

  createNode: (data: Partial<Node>) =>
    api.post<Node>('/Node', data),

  updateNode: (id: number, data: Partial<Node>) =>
    api.put<Node>(`/Node/${id}`, data),

  deleteNode: (id: number) =>
    api.delete(`/Node/${id}`),
};

// Admin APIs
export const adminAPI = {
  getStats: () =>
    api.get<AdminStats>('/Admin/stats'),

  getRobotEfficiency: () =>
    api.get<RobotEfficiency[]>('/Admin/analytics/robot-efficiency'),

  exportDeliveryHistory: () =>
    api.get('/Admin/export/delivery-history', { responseType: 'blob' }),

  createBackup: () =>
    api.post('/Admin/backup'),
};

export default api;
