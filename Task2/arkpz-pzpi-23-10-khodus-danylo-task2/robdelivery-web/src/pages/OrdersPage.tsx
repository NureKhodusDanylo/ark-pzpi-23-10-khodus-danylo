import React from 'react';
import { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Package,
  Send,
  Inbox,
  Filter,
  X,
  MapPin,
  Calendar,
  DollarSign,
  Bot,
  User,
  Play,
  Loader,
} from 'lucide-react';
import Layout from '../components/Layout';
import { orderAPI } from '../lib/api';
import { useAuthStore } from '../store/authStore';
import type { Order } from '../types';
import './OrdersPage.css';

const OrdersPage = () => {
  const user = useAuthStore((state) => state.user);
  const queryClient = useQueryClient();
  const [filter, setFilter] = useState<'all' | 'sent' | 'received'>('all');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [selectedOrder, setSelectedOrder] = useState<Order | null>(null);

  const { data: myOrders, isLoading } = useQuery({
    queryKey: ['orders', 'my-orders'],
    queryFn: async () => {
      const response = await orderAPI.getMyOrders();
      return response.data;
    },
  });

  const sentOrders = myOrders?.filter((order) => order.senderId === user?.id) || [];
  const receivedOrders = myOrders?.filter((order) => order.recipientId === user?.id) || [];

  const cancelOrderMutation = useMutation({
    mutationFn: orderAPI.cancelOrder,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['orders'] });
      setSelectedOrder(null);
      alert('Order cancelled successfully');
    },
    onError: (error: any) => {
      alert(error.response?.data?.error || 'Failed to cancel order');
    },
  });

  const executeOrderMutation = useMutation({
    mutationFn: orderAPI.executeOrder,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['orders'] });
      setSelectedOrder(null);
      alert('Order executed successfully! Robot assigned and en route.');
    },
    onError: (error: any) => {
      alert(error.response?.data?.error || 'Failed to execute order');
    },
  });

  const getFilteredOrders = () => {
    let orders: Order[] = [];

    if (filter === 'all') {
      orders = [...(sentOrders || []), ...(receivedOrders || [])];
    } else if (filter === 'sent') {
      orders = sentOrders || [];
    } else {
      orders = receivedOrders || [];
    }

    if (statusFilter !== 'all') {
      orders = orders.filter((o) => o.status === statusFilter);
    }

    return orders.sort(
      (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    );
  };

  const filteredOrders = getFilteredOrders();

  const getStatusColor = (status: string) => {
    const colors: Record<string, string> = {
      Pending: 'warning',
      Processing: 'info',
      EnRoute: 'primary',
      Delivered: 'success',
      Cancelled: 'error',
    };
    return colors[status] || 'info';
  };

  return (
    <Layout>
      <div className="orders-page">
        <div className="page-header">
          <div>
            <h1 className="page-title">
              My <span className="gradient-text">Orders</span>
            </h1>
            <p className="page-subtitle">Track and manage your deliveries</p>
          </div>
        </div>

        <div className="filters-bar">
          <div className="filter-group">
            <button
              className={`filter-btn ${filter === 'all' ? 'active' : ''}`}
              onClick={() => setFilter('all')}
            >
              <Package size={18} />
              All Orders
            </button>
            <button
              className={`filter-btn ${filter === 'sent' ? 'active' : ''}`}
              onClick={() => setFilter('sent')}
            >
              <Send size={18} />
              Sent
            </button>
            <button
              className={`filter-btn ${filter === 'received' ? 'active' : ''}`}
              onClick={() => setFilter('received')}
            >
              <Inbox size={18} />
              Received
            </button>
          </div>

          <select
            className="status-filter"
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
          >
            <option value="all">All Status</option>
            <option value="Pending">Pending</option>
            <option value="Processing">Processing</option>
            <option value="EnRoute">En Route</option>
            <option value="Delivered">Delivered</option>
            <option value="Cancelled">Cancelled</option>
          </select>
        </div>

        {isLoading ? (
          <div className="loading-state">
            <div className="spinner"></div>
            <p>Loading orders...</p>
          </div>
        ) : filteredOrders.length === 0 ? (
          <div className="empty-state">
            <Package size={64} className="empty-icon" />
            <h3>No orders found</h3>
            <p>Try adjusting your filters or create a new order</p>
          </div>
        ) : (
          <div className="orders-grid">
            {filteredOrders.map((order, index) => (
              <motion.div
                key={order.id}
                className="order-card"
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: index * 0.05 }}
                whileHover={{ y: -5 }}
              >
                <div
                  onClick={() => setSelectedOrder(order)}
                  style={{ cursor: 'pointer' }}
                >
                  <div className="order-card-header">
                    <div className="order-type-badge">
                      {order.senderId === user?.id ? (
                        <Send size={16} />
                      ) : (
                        <Inbox size={16} />
                      )}
                      {order.senderId === user?.id ? 'Sent' : 'Received'}
                    </div>
                    <span className={`badge badge-${getStatusColor(order.status)}`}>
                      {order.status}
                    </span>
                  </div>

                  <h3 className="order-card-title">{order.name}</h3>
                  <p className="order-card-description">{order.description}</p>

                  <div className="order-card-meta">
                    <div className="meta-item">
                      <User size={16} />
                      {order.senderId === user?.id
                        ? order.recipientName
                        : order.senderName}
                    </div>
                    <div className="meta-item">
                      <Calendar size={16} />
                      {new Date(order.createdAt).toLocaleDateString()}
                    </div>
                  </div>

                  <div className="order-card-footer">
                    <div className="order-price">
                      <DollarSign size={16} />
                      ${order.deliveryPrice}
                    </div>
                    {order.robotName && (
                      <div className="robot-badge">
                        <Bot size={16} />
                        {order.robotName}
                      </div>
                    )}
                  </div>
                </div>

                {order.senderId === user?.id && order.status === 'Pending' && (
                  <div style={{ marginTop: '12px', display: 'flex', gap: '8px' }}>
                    <button
                      className="btn btn-primary btn-sm"
                      style={{ flex: 1 }}
                      onClick={(e) => {
                        e.stopPropagation();
                        if (
                          window.confirm(
                            'Execute this order? The system will automatically find and assign the optimal robot.'
                          )
                        ) {
                          executeOrderMutation.mutate(order.id);
                        }
                      }}
                      disabled={executeOrderMutation.isPending}
                    >
                      {executeOrderMutation.isPending ? (
                        <>
                          <Loader className="spinner-icon" size={14} />
                          Executing...
                        </>
                      ) : (
                        <>
                          <Play size={14} />
                          Execute
                        </>
                      )}
                    </button>
                    <button
                      className="btn btn-error btn-sm"
                      style={{ flex: 1 }}
                      onClick={(e) => {
                        e.stopPropagation();
                        if (window.confirm('Are you sure you want to cancel this order?')) {
                          cancelOrderMutation.mutate(order.id);
                        }
                      }}
                      disabled={cancelOrderMutation.isPending}
                    >
                      {cancelOrderMutation.isPending ? 'Cancelling...' : 'Cancel'}
                    </button>
                  </div>
                )}
              </motion.div>
            ))}
          </div>
        )}

        <AnimatePresence>
          {selectedOrder && (
            <motion.div
              className="modal-overlay"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              onClick={() => setSelectedOrder(null)}
            >
              <motion.div
                className="modal-content"
                initial={{ scale: 0.9, opacity: 0 }}
                animate={{ scale: 1, opacity: 1 }}
                exit={{ scale: 0.9, opacity: 0 }}
                onClick={(e) => e.stopPropagation()}
              >
                <div className="modal-header">
                  <h2>Order Details</h2>
                  <button
                    className="close-btn"
                    onClick={() => setSelectedOrder(null)}
                  >
                    <X size={24} />
                  </button>
                </div>

                <div className="modal-body">
                  <div className="detail-section">
                    <h3>Package Information</h3>
                    <div className="detail-row">
                      <span>Name:</span>
                      <strong>{selectedOrder.name}</strong>
                    </div>
                    <div className="detail-row">
                      <span>Description:</span>
                      <strong>{selectedOrder.description}</strong>
                    </div>
                    <div className="detail-row">
                      <span>Weight:</span>
                      <strong>{selectedOrder.weight} kg</strong>
                    </div>
                    <div className="detail-row">
                      <span>Status:</span>
                      <span className={`badge badge-${getStatusColor(selectedOrder.status)}`}>
                        {selectedOrder.status}
                      </span>
                    </div>
                  </div>

                  <div className="detail-section">
                    <h3>Delivery Information</h3>
                    <div className="detail-row">
                      <span>From:</span>
                      <strong>{selectedOrder.senderName}</strong>
                    </div>
                    <div className="detail-row">
                      <span>To:</span>
                      <strong>{selectedOrder.recipientName}</strong>
                    </div>
                    <div className="detail-row">
                      <span>Pickup:</span>
                      <strong>{selectedOrder.pickupNodeName}</strong>
                    </div>
                    <div className="detail-row">
                      <span>Dropoff:</span>
                      <strong>{selectedOrder.dropoffNodeName}</strong>
                    </div>
                  </div>

                  <div className="detail-section">
                    <h3>Pricing</h3>
                    <div className="detail-row">
                      <span>Delivery Price:</span>
                      <strong>${selectedOrder.deliveryPrice}</strong>
                    </div>
                    <div className="detail-row">
                      <span>Product Price:</span>
                      <strong>${selectedOrder.productPrice}</strong>
                    </div>
                    <div className="detail-row">
                      <span>Product Paid:</span>
                      <strong>{selectedOrder.isProductPaid ? 'Yes' : 'No'}</strong>
                    </div>
                  </div>

                  {selectedOrder.robotName && (
                    <div className="detail-section">
                      <h3>Assigned Robot</h3>
                      <div className="detail-row">
                        <span>Robot:</span>
                        <strong>{selectedOrder.robotName}</strong>
                      </div>
                    </div>
                  )}
                </div>

                <div className="modal-footer">
                  {selectedOrder.senderId === user?.id &&
                    selectedOrder.status === 'Pending' && (
                      <>
                        <button
                          className="btn btn-primary"
                          onClick={() => {
                            if (
                              window.confirm(
                                'Execute this order? The system will automatically find and assign the optimal robot.'
                              )
                            ) {
                              executeOrderMutation.mutate(selectedOrder.id);
                            }
                          }}
                          disabled={executeOrderMutation.isPending}
                        >
                          {executeOrderMutation.isPending ? (
                            <>
                              <Loader className="spinner-icon" size={16} />
                              Executing...
                            </>
                          ) : (
                            <>
                              <Play size={16} />
                              Execute Order
                            </>
                          )}
                        </button>
                        <button
                          className="btn btn-error"
                          onClick={() => {
                            if (
                              window.confirm('Are you sure you want to cancel this order?')
                            ) {
                              cancelOrderMutation.mutate(selectedOrder.id);
                            }
                          }}
                          disabled={cancelOrderMutation.isPending}
                        >
                          {cancelOrderMutation.isPending ? 'Cancelling...' : 'Cancel Order'}
                        </button>
                      </>
                    )}
                  {selectedOrder.senderId === user?.id &&
                    selectedOrder.status === 'Processing' &&
                    !selectedOrder.robotName && (
                      <button
                        className="btn btn-error"
                        onClick={() => {
                          if (
                            window.confirm('Are you sure you want to cancel this order?')
                          ) {
                            cancelOrderMutation.mutate(selectedOrder.id);
                          }
                        }}
                        disabled={cancelOrderMutation.isPending}
                      >
                        {cancelOrderMutation.isPending ? 'Cancelling...' : 'Cancel Order'}
                      </button>
                    )}
                  <button
                    className="btn btn-secondary"
                    onClick={() => setSelectedOrder(null)}
                  >
                    Close
                  </button>
                </div>
              </motion.div>
            </motion.div>
          )}
        </AnimatePresence>
      </div>
    </Layout>
  );
};

export default OrdersPage;
