import random
import sys
import traceback

import tensorflow as tf
import numpy as np
from tensorflow import keras
from tensorflow.keras import layers

import socket

import json
import msvcrt as m

class Config:
	def __init__(self, json):
		self.state_count = json['StateCount']
		self.action_count = json['ActionCount']

class Observation:
	def __init__(self, json):
		self.state = json['State']
		self.reward = json['Reward']

HOST = '127.0.0.1'
PORT = 65432
PREVIOUS_STATES_FEED = 7

optimizer = keras.optimizers.Adam(learning_rate=0.00025, clipnorm=1.0)
loss_function = keras.losses.Huber()

gamma = 0.4  # discount factor for past rewards
update_target_network = 100 # interval for updating target network

def setup_client_conn():
	client_conn_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
	client_conn_socket.bind((HOST, PORT))
	client_conn_socket.settimeout(1)
	client_conn_socket.listen()

	while True:
		try:
			conn, addr = client_conn_socket.accept()
			break
		except:
			pass

	return conn

def get_object_from_client(conn):
	data = conn.recv(1024)
	data = data.decode('utf-8')
	jsonData = json.loads(str(data))
	return jsonData

def send_action_id_to_client(conn, action):
	conn.send(bytes(str(action), 'utf8'))

def ping_client(conn):
	conn.send(bytes(str(1), 'utf8'))

def build_network():
	repr_bits = representable_bits_count(config.state_count - 1)
	inputs = keras.Input(shape=(PREVIOUS_STATES_FEED,repr_bits))
	x = layers.Dense(128, activation="relu")(inputs)
	x = layers.Dense(128, activation="relu")(x)
	x = layers.Dense(128, activation="relu")(x)
	outputs = layers.Dense(config.action_count, activation="linear")(x)

	return keras.Model(inputs=inputs, outputs=outputs)

exp_states = []
exp_rewards = []
exp_action_ids = []
exp_next_states = []

def main():

	print('awaiting connection')
	client_conn = setup_client_conn()
	
	print('awaiting config')
	global config
	config = Config(get_object_from_client(client_conn))
	print(f'received config: {config.state_count}, {config.action_count}')

	network = build_network()
	target_network = build_network()
	print(f'network setup done')

	print('awaiting initial observation')
	obs = Observation(get_object_from_client(client_conn))

	i = 0
	while True:
		print(f'iter {i}, selecting action')
		valid_action = False
		while not valid_action:			
			action_id = random.randint(0,config.action_count - 1)
			send_action_id_to_client(client_conn, action_id)
			resp = get_object_from_client(client_conn)
			valid_action = resp['ActionValid']
			print(f'action valid: {valid_action}')

		print('awaiting NEW observation')
		obsnew = Observation(get_object_from_client(client_conn))
		print(f'newobs: {obsnew.state}, {obsnew.reward}')

		# save experience
		exp_states.append(obs.state)
		exp_rewards.append(obsnew.reward)
		exp_action_ids.append(action_id)
		exp_next_states.append(obsnew.state)
		if len(exp_states) > PREVIOUS_STATES_FEED:
			# remove old experience
			del exp_states[0]
			del exp_rewards[0]
			del exp_action_ids[0]
			del exp_next_states[0]

			train(network, target_network, i)
		
		obs = obsnew
		ping_client(client_conn)
		i = i + 1

'''
	returns how many bits are needed to represent 'num'
	i.e. to represent 3 one needs 2 bits
'''
def representable_bits_count(num):
	return len(bin(num)[2:])

'''
	converts list of states in int form
	into input tensor of bit vectors
'''
def convert_state_input(states):
	repr_bits = representable_bits_count(config.state_count - 1)
	fmt_spec = f'0{repr_bits}b'
	input = np.array([ [int(x) for x in format(st, fmt_spec)] for st in states ])
	return input

def train(network, target_network, i):

	next_states_input = convert_state_input(exp_next_states)
	future_rewards = target_network.predict(next_states_input)

	updated_q_values = exp_rewards + gamma * tf.reduce_max(future_rewards, axis=1)

	masks = tf.one_hot(exp_action_ids, config.action_count)
	with tf.GradientTape() as tape:
		states_input = convert_state_input(exp_states)
		q_values = network(states_input)
		q_action = tf.reduce_sum(tf.multiply(q_values, masks), axis=1)
		loss = loss_function(updated_q_values, q_action)
		print(f'loss: {loss}')
		
	grads = tape.gradient(loss, network.trainable_variables)
	optimizer.apply_gradients(zip(grads, network.trainable_variables))
	if i % update_target_network == 0:
		print(f'updating target network {i}%{update_target_network}')
		target_network.set_weights(network.get_weights())

	# debug outputs for all states
	states_input = convert_state_input([st for st in range(config.state_count)])
	q_values = network(states_input, training=False)
	print(f'qvalues for all states in primary network:\n{q_values}')


if __name__ == "__main__":
	try:
		main()
	except Exception as e:
		traceback.print_exc()

	m.getch()
