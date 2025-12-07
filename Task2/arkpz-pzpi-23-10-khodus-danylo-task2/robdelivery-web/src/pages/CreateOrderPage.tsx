import React from 'react';
import { useState } from 'react';
import { motion } from 'framer-motion';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery } from '@tanstack/react-query';
import { Package, User, DollarSign, Weight, FileText, Loader, Search, Image, X } from 'lucide-react';
import Layout from '../components/Layout';
import { orderAPI, userAPI } from '../lib/api';
import './CreateOrderPage.css';

const CreateOrderPage = () => {
  const navigate = useNavigate();
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedRecipient, setSelectedRecipient] = useState<any>(null);
  const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    weight: '',
    productPrice: '',
    isProductPaid: false,
  });

  const { data: users } = useQuery({
    queryKey: ['users', 'search', searchQuery],
    queryFn: async () => {
      if (!searchQuery) return [];
      const response = await userAPI.searchUsers(searchQuery);
      return response.data;
    },
    enabled: searchQuery.length > 0,
  });

  const createOrderMutation = useMutation({
    mutationFn: orderAPI.createOrder,
    onSuccess: () => {
      alert('Order created successfully!');
      navigate('/orders');
    },
    onError: (error: any) => {
      alert(error.response?.data?.message || 'Failed to create order');
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedRecipient) {
      alert('Please select a recipient');
      return;
    }

    createOrderMutation.mutate({
      recipientId: selectedRecipient.id,
      name: formData.name,
      description: formData.description,
      weight: parseFloat(formData.weight),
      productPrice: parseFloat(formData.productPrice),
      isProductPaid: formData.isProductPaid,
      deliveryPayer: 0,
      files: selectedFiles,
    });
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) {
      setSelectedFiles(Array.from(e.target.files));
    }
  };

  const removeFile = (index: number) => {
    setSelectedFiles((prev) => prev.filter((_, i) => i !== index));
  };

  const estimatedDeliveryPrice = formData.weight
    ? (50 + parseFloat(formData.weight) * 10).toFixed(2)
    : '0.00';

  return (
    <Layout>
      <div className="create-order-page">
        <motion.div
          className="page-header"
          initial={{ opacity: 0, y: -20 }}
          animate={{ opacity: 1, y: 0 }}
        >
          <h1 className="page-title">
            Create <span className="gradient-text">New Order</span>
          </h1>
          <p className="page-subtitle">Fill in the details to send your package</p>
        </motion.div>

        <motion.form
          className="order-form"
          onSubmit={handleSubmit}
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.2 }}
        >
          <div className="form-section">
            <h2 className="section-title">
              <User size={24} />
              Recipient
            </h2>

            <div className="form-group">
              <label className="form-label">Search Recipient</label>
              <div className="search-input-wrapper">
                <Search size={20} className="search-icon" />
                <input
                  type="text"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="form-input"
                  placeholder="Search by name, phone, or address..."
                />
              </div>
              {users && users.length > 0 && (
                <div className="search-results">
                  {users.map((user) => (
                    <div
                      key={user.id}
                      className={`search-result-item ${
                        selectedRecipient?.id === user.id ? 'selected' : ''
                      }`}
                      onClick={() => {
                        setSelectedRecipient(user);
                        setSearchQuery('');
                      }}
                    >
                      <div className="user-avatar">{user.userName.charAt(0).toUpperCase()}</div>
                      <div>
                        <p className="user-name">{user.userName}</p>
                        <p className="user-details">{user.phoneNumber} • {user.address}</p>
                      </div>
                    </div>
                  ))}
                </div>
              )}
              {selectedRecipient && (
                <div className="selected-recipient">
                  <div className="user-avatar">{selectedRecipient.userName.charAt(0).toUpperCase()}</div>
                  <div>
                    <p className="user-name">{selectedRecipient.userName}</p>
                    <p className="user-details">{selectedRecipient.address}</p>
                  </div>
                  <button
                    type="button"
                    onClick={() => setSelectedRecipient(null)}
                    className="btn btn-ghost btn-sm"
                  >
                    Change
                  </button>
                </div>
              )}
            </div>
          </div>

          <div className="form-section">
            <h2 className="section-title">
              <Package size={24} />
              Package Details
            </h2>

            <div className="form-group">
              <label className="form-label">Package Name</label>
              <input
                type="text"
                required
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                className="form-input"
                placeholder="Electronics, Documents, etc."
              />
            </div>

            <div className="form-group">
              <label className="form-label">
                <FileText size={18} />
                Description
              </label>
              <textarea
                required
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                className="form-textarea"
                placeholder="Describe the package contents..."
                rows={3}
              />
            </div>

            <div className="form-row">
              <div className="form-group">
                <label className="form-label">
                  <Weight size={18} />
                  Weight (kg)
                </label>
                <input
                  type="number"
                  step="0.1"
                  required
                  value={formData.weight}
                  onChange={(e) => setFormData({ ...formData, weight: e.target.value })}
                  className="form-input"
                  placeholder="0.0"
                />
              </div>

              <div className="form-group">
                <label className="form-label">
                  <DollarSign size={18} />
                  Product Price
                </label>
                <input
                  type="number"
                  step="0.01"
                  required
                  value={formData.productPrice}
                  onChange={(e) => setFormData({ ...formData, productPrice: e.target.value })}
                  className="form-input"
                  placeholder="0.00"
                />
              </div>
            </div>

            <div className="form-group">
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  checked={formData.isProductPaid}
                  onChange={(e) => setFormData({ ...formData, isProductPaid: e.target.checked })}
                  className="checkbox-input"
                />
                Product is already paid
              </label>
            </div>

            <div className="form-group">
              <label className="form-label">
                <Image size={18} />
                Package Images (Optional)
              </label>
              <input
                type="file"
                multiple
                accept="image/*"
                onChange={handleFileChange}
                className="form-input"
                style={{ padding: '8px' }}
              />
              {selectedFiles.length > 0 && (
                <div style={{ marginTop: '12px', display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
                  {selectedFiles.map((file, index) => (
                    <div
                      key={index}
                      style={{
                        display: 'flex',
                        alignItems: 'center',
                        gap: '8px',
                        padding: '6px 12px',
                        background: 'rgba(139, 92, 246, 0.1)',
                        borderRadius: '8px',
                        fontSize: '14px',
                      }}
                    >
                      <Image size={16} />
                      <span>{file.name}</span>
                      <button
                        type="button"
                        onClick={() => removeFile(index)}
                        style={{
                          background: 'none',
                          border: 'none',
                          cursor: 'pointer',
                          padding: '2px',
                          display: 'flex',
                          alignItems: 'center',
                        }}
                      >
                        <X size={16} />
                      </button>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>

          <div className="form-summary">
            <div className="summary-item">
              <span>Delivery Price:</span>
              <span className="summary-value">${estimatedDeliveryPrice}</span>
            </div>
            <div className="summary-item total">
              <span>Total:</span>
              <span className="summary-value">
                ${(
                  parseFloat(estimatedDeliveryPrice) +
                  (formData.isProductPaid ? 0 : parseFloat(formData.productPrice || '0'))
                ).toFixed(2)}
              </span>
            </div>
          </div>

          <motion.button
            type="submit"
            className="btn btn-primary btn-large btn-full"
            disabled={createOrderMutation.isPending}
            whileHover={{ scale: 1.02 }}
            whileTap={{ scale: 0.98 }}
          >
            {createOrderMutation.isPending ? (
              <>
                <Loader className="spinner-icon" size={20} />
                Creating Order...
              </>
            ) : (
              <>
                <Package size={20} />
                Create Order
              </>
            )}
          </motion.button>
        </motion.form>
      </div>
    </Layout>
  );
};

export default CreateOrderPage;
