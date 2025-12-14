import React, { useEffect, useState } from 'react';
import { APIProvider, Map, AdvancedMarker, Pin } from '@vis.gl/react-google-maps';
import * as signalR from '@microsoft/signalr';
import { mapAPI } from '../lib/api';
import { RobotMapPosition, NodeMapPosition } from '../types';
import Layout from '../components/Layout';
import { useAuthStore } from '../store/authStore';

const GOOGLE_MAPS_API_KEY = 'AIzaSyBhjS33pPRKgHt2DZEbbyXGvv-yjbmnUwc'; // Replace with your actual API key
const SIGNALR_HUB_URL = 'http://35.192.164.131:5102/hubs/map';

// Default center (можно изменить на ваше местоположение)
const DEFAULT_CENTER = { lat: 50.4501, lng: 30.5234 }; // Kyiv, Ukraine

function MapPage() {
  const [robots, setRobots] = useState<RobotMapPosition[]>([]);
  const [nodes, setNodes] = useState<NodeMapPosition[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [hubConnection, setHubConnection] = useState<signalR.HubConnection | null>(null);
  const token = useAuthStore((state) => state.token);

  // Загрузка начальных данных карты
  useEffect(() => {
    loadMapData();
  }, []);

  // Настройка SignalR подключения
  useEffect(() => {
    if (!token) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(SIGNALR_HUB_URL, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .build();

    connection
      .start()
      .then(() => {
        console.log('SignalR Connected');
        setHubConnection(connection);
      })
      .catch((err) => console.error('SignalR Connection Error: ', err));

    // Обработчики обновлений
    connection.on('ReceiveRobotUpdate', (robotUpdate: RobotMapPosition) => {
      setRobots((prevRobots) => {
        const index = prevRobots.findIndex((r) => r.id === robotUpdate.id);
        if (index !== -1) {
          const updated = [...prevRobots];
          updated[index] = robotUpdate;
          return updated;
        } else {
          return [...prevRobots, robotUpdate];
        }
      });
    });

    connection.on('ReceiveNodeUpdate', (nodeUpdate: NodeMapPosition) => {
      setNodes((prevNodes) => {
        const index = prevNodes.findIndex((n) => n.id === nodeUpdate.id);
        if (index !== -1) {
          const updated = [...prevNodes];
          updated[index] = nodeUpdate;
          return updated;
        } else {
          return [...prevNodes, nodeUpdate];
        }
      });
    });

    return () => {
      connection.stop();
    };
  }, [token]);

  const loadMapData = async () => {
    try {
      setLoading(true);
      const response = await mapAPI.getMapData();
      setRobots(response.data.robots);
      setNodes(response.data.nodes);
      setError(null);
    } catch (err) {
      console.error('Error loading map data:', err);
      setError('Failed to load map data');
    } finally {
      setLoading(false);
    }
  };

  // Цвета маркеров для разных типов узлов
  const getNodeColor = (type: string): string => {
    switch (type) {
      case 'ChargingStation':
        return '#10b981'; // Green
      case 'Depot':
        return '#3b82f6'; // Blue
      case 'UserNode':
        return '#8b5cf6'; // Purple
      default:
        return '#6b7280'; // Gray
    }
  };

  // Цвета маркеров для разных статусов роботов
  const getRobotColor = (status: string): string => {
    switch (status) {
      case 'Idle':
        return '#22c55e'; // Green
      case 'Delivering':
        return '#f59e0b'; // Orange
      case 'Charging':
        return '#3b82f6'; // Blue
      case 'Maintenance':
        return '#ef4444'; // Red
      default:
        return '#6b7280'; // Gray
    }
  };

  // Получить иконку для типа робота
  const getRobotIcon = (type: string): string => {
    return type === 'Aerial' ? '🚁' : '🤖';
  };

  return (
    <Layout>
      <div className="p-8">
        <div className="flex justify-between items-center mb-6">
          <h1 className="text-3xl font-bold">Delivery Map</h1>
          <button
            onClick={loadMapData}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
          >
            Refresh Data
          </button>
        </div>

        {error && (
          <div className="mb-4 p-4 bg-red-100 border border-red-400 text-red-700 rounded">
            {error}
          </div>
        )}

        {loading ? (
          <div className="flex justify-center items-center h-96">
            <div className="text-xl">Loading map...</div>
          </div>
        ) : (
          <div className="space-y-4">
            {/* Легенда */}
            <div className="bg-white p-4 rounded-lg shadow">
              <h2 className="text-lg font-semibold mb-3">Legend</h2>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <div>
                  <h3 className="font-medium mb-2">Nodes</h3>
                  <div className="space-y-1 text-sm">
                    <div className="flex items-center gap-2">
                      <div className="w-3 h-3 rounded-full bg-green-500"></div>
                      <span>Charging Station</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <div className="w-3 h-3 rounded-full bg-blue-500"></div>
                      <span>Depot</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <div className="w-3 h-3 rounded-full bg-purple-500"></div>
                      <span>User Node</span>
                    </div>
                  </div>
                </div>
                <div>
                  <h3 className="font-medium mb-2">Robot Status</h3>
                  <div className="space-y-1 text-sm">
                    <div className="flex items-center gap-2">
                      <div className="w-3 h-3 rounded-full bg-green-500"></div>
                      <span>Idle</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <div className="w-3 h-3 rounded-full bg-orange-500"></div>
                      <span>Delivering</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <div className="w-3 h-3 rounded-full bg-blue-500"></div>
                      <span>Charging</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <div className="w-3 h-3 rounded-full bg-red-500"></div>
                      <span>Maintenance</span>
                    </div>
                  </div>
                </div>
                <div className="col-span-2">
                  <h3 className="font-medium mb-2">Statistics</h3>
                  <div className="grid grid-cols-2 gap-2 text-sm">
                    <div>Total Robots: <strong>{robots.length}</strong></div>
                    <div>Total Nodes: <strong>{nodes.length}</strong></div>
                    <div>Active Deliveries: <strong>{robots.filter(r => r.status === 'Delivering').length}</strong></div>
                    <div>Idle Robots: <strong>{robots.filter(r => r.status === 'Idle').length}</strong></div>
                  </div>
                </div>
              </div>
            </div>

            {/* Карта */}
            <div className="bg-white rounded-lg shadow overflow-hidden">
              <APIProvider apiKey={GOOGLE_MAPS_API_KEY}>
                <Map
                  style={{ width: '100%', height: '600px' }}
                  defaultCenter={DEFAULT_CENTER}
                  defaultZoom={12}
                  mapId="delivery-map"
                >
                  {/* Маркеры узлов */}
                  {nodes.map((node) => (
                    <AdvancedMarker
                      key={`node-${node.id}`}
                      position={{ lat: node.latitude, lng: node.longitude }}
                      title={`${node.name} (${node.typeName})`}
                    >
                      <Pin
                        background={getNodeColor(node.type)}
                        borderColor={'white'}
                        glyphColor={'white'}
                      />
                    </AdvancedMarker>
                  ))}

                  {/* Маркеры роботов */}
                  {robots
                    .filter((robot) => robot.latitude && robot.longitude)
                    .map((robot) => (
                      <AdvancedMarker
                        key={`robot-${robot.id}`}
                        position={{ lat: robot.latitude!, lng: robot.longitude! }}
                        title={`${robot.name} - ${robot.statusName} (${robot.batteryLevel}%)`}
                      >
                        <div className="flex flex-col items-center">
                          <Pin
                            background={getRobotColor(robot.status)}
                            borderColor={'white'}
                            glyphColor={'white'}
                          />
                          <div className="bg-white px-2 py-1 rounded shadow text-xs mt-1">
                            {getRobotIcon(robot.type)} {robot.name}
                          </div>
                        </div>
                      </AdvancedMarker>
                    ))}
                </Map>
              </APIProvider>
            </div>

            {/* Список роботов */}
            <div className="bg-white p-4 rounded-lg shadow">
              <h2 className="text-lg font-semibold mb-3">Active Robots</h2>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
                {robots.map((robot) => (
                  <div
                    key={robot.id}
                    className="border rounded p-3 hover:shadow-md transition-shadow"
                  >
                    <div className="flex items-center justify-between mb-2">
                      <span className="font-medium">
                        {getRobotIcon(robot.type)} {robot.name}
                      </span>
                      <span
                        className="px-2 py-1 rounded text-xs text-white"
                        style={{ backgroundColor: getRobotColor(robot.status) }}
                      >
                        {robot.statusName}
                      </span>
                    </div>
                    <div className="text-sm space-y-1 text-gray-600">
                      <div>Battery: {robot.batteryLevel}%</div>
                      <div>Type: {robot.typeName}</div>
                      {robot.currentNodeName && (
                        <div>Location: {robot.currentNodeName}</div>
                      )}
                      {robot.targetNodeName && (
                        <div>Target: {robot.targetNodeName}</div>
                      )}
                      <div>Active Orders: {robot.activeOrdersCount}</div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}
      </div>
    </Layout>
  );
}

export default MapPage;
