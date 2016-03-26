Ticket client => Socket => Agent(on server)
Agent
1.pub message to redis
2.listen the redis
3.response to the client

Server:
1.listen the reids and get new message
2.logic process
3.push message to redis
