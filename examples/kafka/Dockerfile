# Single-node KRaft Kafka from the Apache release on Microsoft's OpenJDK image
# (avoids Docker Hub pulls: rate limits and proxies that block large layers).
FROM mcr.microsoft.com/openjdk/jdk:21-ubuntu
ADD https://archive.apache.org/dist/kafka/3.9.1/kafka_2.13-3.9.1.tgz /tmp/kafka.tgz
RUN mkdir /opt/kafka && tar -xzf /tmp/kafka.tgz -C /opt/kafka --strip-components=1 && rm /tmp/kafka.tgz \
 && printf '%s\n' \
      'process.roles=broker,controller' \
      'node.id=1' \
      'controller.quorum.voters=1@localhost:9093' \
      'listeners=PLAINTEXT://:9092,CONTROLLER://:9093' \
      'advertised.listeners=PLAINTEXT://kafka:9092' \
      'controller.listener.names=CONTROLLER' \
      'listener.security.protocol.map=CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT' \
      'log.dirs=/tmp/kraft-logs' \
      'offsets.topic.replication.factor=1' \
      'transaction.state.log.replication.factor=1' \
      'transaction.state.log.min.isr=1' \
      'group.initial.rebalance.delay.ms=0' \
      > /opt/kafka/config/example.properties
ENV KAFKA_HEAP_OPTS="-Xmx256m -Xms256m"
CMD /opt/kafka/bin/kafka-storage.sh format -t $(/opt/kafka/bin/kafka-storage.sh random-uuid) -c /opt/kafka/config/example.properties \
 && exec /opt/kafka/bin/kafka-server-start.sh /opt/kafka/config/example.properties
