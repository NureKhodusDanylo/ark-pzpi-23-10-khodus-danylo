import React from 'react';
import { motion } from 'framer-motion';
import { useNavigate } from 'react-router-dom';
import {
  Package,
  Zap,
  Shield,
  MapPin,
  Bot,
  Rocket,
  TrendingUp,
  Globe,
  CheckCircle,
  ArrowRight,
} from 'lucide-react';
import './LandingPage.css';

const LandingPage = () => {
  const navigate = useNavigate();

  const features = [
    {
      icon: <Bot size={40} />,
      title: 'Autonomous Robots',
      description: 'Advanced ground and aerial robots for fast delivery',
    },
    {
      icon: <Zap size={40} />,
      title: 'Lightning Fast',
      description: 'Get your packages delivered in record time',
    },
    {
      icon: <Shield size={40} />,
      title: 'Secure & Safe',
      description: 'End-to-end encryption and secure delivery',
    },
    {
      icon: <MapPin size={40} />,
      title: 'Real-Time Tracking',
      description: 'Track your delivery every step of the way',
    },
  ];

  const stats = [
    { value: '10k+', label: 'Deliveries' },
    { value: '99.9%', label: 'Success Rate' },
    { value: '50+', label: 'Robots' },
    { value: '24/7', label: 'Availability' },
  ];

  const benefits = [
    'Contactless delivery',
    'Eco-friendly transportation',
    'Real-time GPS tracking',
    'Automated route optimization',
    'Weather-resistant robots',
    'Instant delivery confirmation',
  ];

  return (
    <div className="landing-page">
      {/* Navigation */}
      <motion.nav
        className="nav"
        initial={{ y: -100 }}
        animate={{ y: 0 }}
        transition={{ duration: 0.8 }}
      >
        <div className="container nav-content">
          <div className="logo">
            <Bot className="logo-icon" />
            <span className="gradient-text">RobDelivery</span>
          </div>
          <div className="nav-links">
            <button onClick={() => navigate('/login')} className="btn btn-ghost">
              Login
            </button>
            <button onClick={() => navigate('/register')} className="btn btn-primary">
              Get Started
            </button>
          </div>
        </div>
      </motion.nav>

      {/* Hero Section */}
      <section className="hero">
        <div className="hero-background">
          <div className="gradient-orb orb-1"></div>
          <div className="gradient-orb orb-2"></div>
          <div className="gradient-orb orb-3"></div>
        </div>

        <div className="container hero-content">
          <motion.div
            className="hero-text"
            initial={{ opacity: 0, y: 50 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 1 }}
          >
            <motion.h1
              className="hero-title"
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.2, duration: 0.8 }}
            >
              Future of Delivery
              <br />
              <span className="gradient-text">Powered by Robots</span>
            </motion.h1>

            <motion.p
              className="hero-subtitle"
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.4, duration: 0.8 }}
            >
              Experience autonomous last-mile delivery with our advanced robotic fleet.
              Fast, secure, and eco-friendly.
            </motion.p>

            <motion.div
              className="hero-cta"
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.6, duration: 0.8 }}
            >
              <button onClick={() => navigate('/register')} className="btn btn-primary btn-large">
                Start Delivery <Rocket size={20} />
              </button>
              <button className="btn btn-secondary btn-large">
                Learn More <ArrowRight size={20} />
              </button>
            </motion.div>
          </motion.div>

          <motion.div
            className="hero-visual"
            initial={{ opacity: 0, scale: 0.8 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ delay: 0.4, duration: 1 }}
          >
            <div className="robot-container">
              <motion.div
                className="robot-icon"
                animate={{
                  y: [0, -20, 0],
                }}
                transition={{
                  duration: 3,
                  repeat: Infinity,
                  ease: 'easeInOut',
                }}
              >
                <Bot size={200} className="robot" />
              </motion.div>
              <div className="orbit-circles">
                <div className="orbit orbit-1">
                  <Package className="orbit-icon" />
                </div>
                <div className="orbit orbit-2">
                  <MapPin className="orbit-icon" />
                </div>
                <div className="orbit orbit-3">
                  <Zap className="orbit-icon" />
                </div>
              </div>
            </div>
          </motion.div>
        </div>
      </section>

      {/* Stats Section */}
      <section className="stats-section">
        <div className="container">
          <div className="stats-grid">
            {stats.map((stat, index) => (
              <motion.div
                key={index}
                className="stat-card"
                initial={{ opacity: 0, y: 30 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true }}
                transition={{ delay: index * 0.1, duration: 0.6 }}
              >
                <h3 className="stat-value gradient-text">{stat.value}</h3>
                <p className="stat-label">{stat.label}</p>
              </motion.div>
            ))}
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section className="features-section">
        <div className="container">
          <motion.div
            className="section-header"
            initial={{ opacity: 0, y: 30 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
          >
            <h2 className="section-title">
              Why Choose <span className="gradient-text">RobDelivery</span>
            </h2>
            <p className="section-subtitle">
              Advanced technology meets exceptional service
            </p>
          </motion.div>

          <div className="features-grid">
            {features.map((feature, index) => (
              <motion.div
                key={index}
                className="feature-card glass-effect"
                initial={{ opacity: 0, y: 30 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true }}
                transition={{ delay: index * 0.1, duration: 0.6 }}
                whileHover={{ scale: 1.05, y: -5 }}
              >
                <div className="feature-icon">{feature.icon}</div>
                <h3 className="feature-title">{feature.title}</h3>
                <p className="feature-description">{feature.description}</p>
              </motion.div>
            ))}
          </div>
        </div>
      </section>

      {/* Benefits Section */}
      <section className="benefits-section">
        <div className="container">
          <div className="benefits-content">
            <motion.div
              className="benefits-visual"
              initial={{ opacity: 0, x: -50 }}
              whileInView={{ opacity: 1, x: 0 }}
              viewport={{ once: true }}
            >
              <div className="benefits-graphic">
                <Globe size={300} className="globe-icon" />
                <motion.div
                  className="pulse-ring"
                  animate={{
                    scale: [1, 1.5],
                    opacity: [0.5, 0],
                  }}
                  transition={{
                    duration: 2,
                    repeat: Infinity,
                    ease: 'easeOut',
                  }}
                />
              </div>
            </motion.div>

            <motion.div
              className="benefits-list"
              initial={{ opacity: 0, x: 50 }}
              whileInView={{ opacity: 1, x: 0 }}
              viewport={{ once: true }}
            >
              <h2 className="benefits-title">
                Built for the <span className="gradient-text">Smart City</span>
              </h2>
              <p className="benefits-description">
                Our robotic delivery system is designed to integrate seamlessly with modern
                urban infrastructure.
              </p>
              <div className="benefits-items">
                {benefits.map((benefit, index) => (
                  <motion.div
                    key={index}
                    className="benefit-item"
                    initial={{ opacity: 0, x: 20 }}
                    whileInView={{ opacity: 1, x: 0 }}
                    viewport={{ once: true }}
                    transition={{ delay: index * 0.1 }}
                  >
                    <CheckCircle className="check-icon" />
                    <span>{benefit}</span>
                  </motion.div>
                ))}
              </div>
            </motion.div>
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="cta-section">
        <div className="container">
          <motion.div
            className="cta-content glass-effect"
            initial={{ opacity: 0, y: 30 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
          >
            <div className="cta-text">
              <h2 className="cta-title">Ready to Experience the Future?</h2>
              <p className="cta-subtitle">Join thousands of satisfied customers today</p>
            </div>
            <button onClick={() => navigate('/register')} className="btn btn-primary btn-large">
              Get Started Now <TrendingUp size={20} />
            </button>
          </motion.div>
        </div>
      </section>

      {/* Footer */}
      <footer className="footer">
        <div className="container">
          <div className="footer-content">
            <div className="footer-brand">
              <div className="logo">
                <Bot className="logo-icon" />
                <span className="gradient-text">RobDelivery</span>
              </div>
              <p className="footer-description">
                Revolutionizing last-mile delivery with autonomous robots
              </p>
            </div>
            <div className="footer-copyright">
              © 2025 RobDelivery. All rights reserved.
            </div>
          </div>
        </div>
      </footer>
    </div>
  );
};

export default LandingPage;
