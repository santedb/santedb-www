# SanteDB Server Dockerfile  
FROM mono:slim
RUN apt-get update   
RUN apt-get install -y binutils mono-complete ca-certificates-mono referenceassemblies-pcl   
RUN rm -rf /var/lib/apt/lists/* /tmp/*

MAINTAINER "SanteSuite Contributors"
RUN mkdir /santedb
COPY ./bin/Release/ /santedb/
# RUN mv -vf /santedb/Data/*.dataset /santedb/data/
# RUN rm -rf /santedb/Data
WORKDIR /santedb
EXPOSE 9200/tcp
CMD [ "mono", "/santedb/santedb-www.exe", "--dllForce", "--console","--daemon" ]